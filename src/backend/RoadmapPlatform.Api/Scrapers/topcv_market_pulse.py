#!/usr/bin/env python3
import argparse
import html
import json
import re
import sys
import time
import urllib.error
import urllib.parse
import urllib.request
from datetime import datetime, timezone


try:
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    sys.stderr.reconfigure(encoding="utf-8", errors="replace")
except AttributeError:
    pass


BROWSER_HEADERS = {
    "User-Agent": (
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) "
        "AppleWebKit/537.36 (KHTML, like Gecko) "
        "Chrome/125.0 Safari/537.36"
    ),
    "Accept": "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8",
    "Accept-Language": "vi-VN,vi;q=0.9,en-US;q=0.8,en;q=0.7",
    "Cache-Control": "no-cache",
    "Pragma": "no-cache",
    "Upgrade-Insecure-Requests": "1",
}


def log(message):
    print(message, file=sys.stderr)


def build_url(template, keyword, page):
    encoded_keyword = urllib.parse.quote(keyword.strip(), safe="")
    return (
        template.replace("{keyword}", encoded_keyword)
        .replace("{page}", str(page))
        .replace("{1}", str(page))
    )


def request_html(opener, url, timeout, referer=None):
    headers = dict(BROWSER_HEADERS)
    if referer:
        headers["Referer"] = referer

    request = urllib.request.Request(url, headers=headers)
    with opener.open(request, timeout=timeout) as response:
        charset = response.headers.get_content_charset() or "utf-8"
        return response.read().decode(charset, errors="ignore")


def normalize_job_url(base_url, value):
    value = html.unescape(value)
    absolute = urllib.parse.urljoin(base_url, value)
    parsed = urllib.parse.urlsplit(absolute)

    if parsed.scheme not in ("http", "https"):
        return None

    if "/viec-lam/" not in parsed.path or not parsed.path.endswith(".html"):
        return None

    return urllib.parse.urlunsplit((parsed.scheme, parsed.netloc, parsed.path, "", ""))


def extract_job_links(page_html, base_url):
    links = []
    seen = set()

    for match in re.finditer(r"href=[\"']([^\"']+/viec-lam/[^\"']+?\.html(?:\?[^\"']*)?)[\"']", page_html, re.I):
        normalized = normalize_job_url(base_url, match.group(1))

        if normalized and normalized not in seen:
            seen.add(normalized)
            links.append(normalized)

    return links


def strip_html(value):
    value = re.sub(r"<(script|style)[^>]*>.*?</\1>", " ", value, flags=re.I | re.S)
    value = re.sub(r"<br\s*/?>", "\n", value, flags=re.I)
    value = re.sub(r"</(p|li|div|h[1-6])>", "\n", value, flags=re.I)
    value = re.sub(r"<[^>]+>", " ", value)
    value = re.sub(r"<[^>\n]*$", " ", value)
    value = html.unescape(value)
    value = re.sub(r"[ \t\r\f\v]+", " ", value)
    value = re.sub(r"\n\s*\n+", "\n", value)
    return value.strip()


def first_text(patterns, page_html):
    for pattern in patterns:
        match = re.search(pattern, page_html, re.I | re.S)
        if match:
            text = strip_html(match.group(1))
            if text:
                return text
    return None


def extract_description(page_html):
    start_match = re.search(r"<div[^>]+class=[\"'][^\"']*job-description[^\"']*[\"'][^>]*>", page_html, re.I)

    if not start_match:
        return strip_html(page_html)

    start = start_match.start()
    end_candidates = [
        page_html.find('class="job-detail__information-detail--actions"', start),
        page_html.find('class="job-detail__box--right"', start),
        page_html.find('class="job-detail__company"', start),
    ]
    end_candidates = [candidate for candidate in end_candidates if candidate > start]
    end = min(end_candidates) if end_candidates else min(len(page_html), start + 50000)

    return strip_html(page_html[start:end])


def extract_company(page_html):
    company_block_match = re.search(
        r"<div[^>]+class=[\"'][^\"']*company-name[^\"']*[\"'][^>]*>(.*?)</div>",
        page_html,
        re.I | re.S,
    )

    if company_block_match:
        block = company_block_match.group(1)
        title_match = re.search(r"title=[\"']([^\"']+)[\"']", block, re.I)
        if title_match:
            return html.unescape(title_match.group(1)).strip()

        text = strip_html(block)
        if text:
            return text

    return first_text(
        [
            r"<meta\s+property=[\"']og:site_name[\"']\s+content=[\"']([^\"']+)[\"']",
            r"<span[^>]*class=[\"'][^\"']*company[^\"']*[\"'][^>]*>(.*?)</span>",
        ],
        page_html,
    )


def extract_labeled_value(text, label, stop_labels):
    escaped_stops = "|".join(re.escape(stop) for stop in stop_labels)
    pattern = rf"{re.escape(label)}\s*(.*?)(?=\s*(?:{escaped_stops})\s|$)"
    match = re.search(pattern, text, re.I | re.S)

    if not match:
        return None

    value = re.sub(r"\s+", " ", match.group(1)).strip(" :-")
    return value or None


def extract_location(description):
    location = extract_labeled_value(
        description,
        "Địa điểm làm việc",
        ["Thời gian làm việc", "Cách thức ứng tuyển", "Yêu cầu ứng viên", "Quyền lợi"],
    )

    if not location:
        return None

    location = re.sub(r"\(đã được cập nhật.*?\)", " ", location, flags=re.I)
    location = re.sub(r"\s+", " ", location).strip(" -:")
    return location[:160] if location else None


def extract_deadline(page_text):
    patterns = [
        r"Hạn\s+nộp\s+hồ\s+sơ\s*:?\s*(\d{1,2}[/-]\d{1,2}[/-]\d{4})",
        r"Deadline\s*:?\s*(\d{1,2}[/-]\d{1,2}[/-]\d{4})",
    ]

    for pattern in patterns:
        match = re.search(pattern, page_text, re.I)
        if not match:
            continue

        value = match.group(1).replace("-", "/")

        try:
            return datetime.strptime(value, "%d/%m/%Y").replace(tzinfo=timezone.utc).isoformat()
        except ValueError:
            continue

    return None


def clean_title(title):
    title = re.split(r"\s+Nhà tuyển dụng đã được xác thực", title, maxsplit=1, flags=re.I)[0]
    title = re.split(r"\s+data-", title, maxsplit=1, flags=re.I)[0]
    title = re.sub(r"\s+", " ", title).strip(" -")
    return title or "Untitled TopCV job"


def parse_detail(source_name, url, page_html):
    title = clean_title(first_text(
        [
            r"<h1[^>]+class=[\"'][^\"']*job-detail__info--title[^\"']*[\"'][^>]*>(.*?)</h1>",
            r"<h1[^>]*>(.*?)</h1>",
            r"<meta\s+property=[\"']og:title[\"']\s+content=[\"']([^\"']+)[\"']",
        ],
        page_html,
    ) or "Untitled TopCV job")

    detail_text = strip_html(page_html)
    description = extract_description(page_html)
    salary = extract_labeled_value(
        detail_text,
        "Mức lương",
        ["Địa điểm", "Kinh nghiệm", "Cấp bậc", "Số lượng tuyển", "Hình thức làm việc"],
    )
    location = extract_location(description)

    if salary:
        description = f"Mức lương: {salary}\n{description}"

    return {
        "SourceName": source_name,
        "Title": title[:250],
        "CompanyName": extract_company(page_html),
        "Location": location,
        "Url": url,
        "Description": description,
        "PublishedAt": None,
        "ExpiresAt": extract_deadline(detail_text),
    }


def scrape(args):
    cookie_processor = urllib.request.HTTPCookieProcessor()
    opener = urllib.request.build_opener(cookie_processor)
    links = []
    seen = set()

    for page in range(1, args.pages + 1):
        search_url = build_url(args.search_url_template, args.keyword, page)

        try:
            page_html = request_html(opener, search_url, args.timeout, args.base_url)
        except urllib.error.HTTPError as exc:
            log(f"TopCV search returned HTTP {exc.code} for {search_url}")
            continue
        except Exception as exc:
            log(f"TopCV search failed for {search_url}: {exc}")
            continue

        page_links = extract_job_links(page_html, args.base_url)
        log(f"TopCV search page {page} yielded {len(page_links)} candidate job links")

        for link in page_links:
            if link not in seen:
                seen.add(link)
                links.append(link)

            if len(links) >= args.limit:
                break

        if len(links) >= args.limit:
            break

        if args.delay > 0:
            time.sleep(args.delay)

    postings = []

    for index, link in enumerate(links[: args.limit], start=1):
        try:
            detail_html = request_html(opener, link, args.timeout, args.base_url)
            posting = parse_detail(args.source_name, link, detail_html)

            if posting["Description"]:
                postings.append(posting)
                log(f"TopCV detail {index}/{len(links)} parsed: {posting['Title'][:80]}")
        except urllib.error.HTTPError as exc:
            log(f"TopCV detail returned HTTP {exc.code} for {link}")
        except Exception as exc:
            log(f"TopCV detail failed for {link}: {exc}")

        if args.delay > 0 and index < len(links):
            time.sleep(args.delay)

    return postings


def main():
    parser = argparse.ArgumentParser(description="Scrape TopCV job details for Market Pulse.")
    parser.add_argument("--source-name", required=True)
    parser.add_argument("--base-url", required=True)
    parser.add_argument("--search-url-template", required=True)
    parser.add_argument("--keyword", required=True)
    parser.add_argument("--pages", type=int, default=1)
    parser.add_argument("--limit", type=int, default=20)
    parser.add_argument("--delay", type=int, default=2)
    parser.add_argument("--timeout", type=int, default=30)
    args = parser.parse_args()

    started_at = datetime.now(timezone.utc).isoformat()
    log(f"TopCV scrape started at {started_at}; keyword={args.keyword!r}; pages={args.pages}; limit={args.limit}")
    postings = scrape(args)
    log(f"TopCV scrape finished with {len(postings)} normalized postings")
    print(json.dumps(postings, ensure_ascii=True))


if __name__ == "__main__":
    main()
