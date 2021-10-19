use regex::RegexBuilder;
use reqwest::redirect::Policy;
use reqwest::StatusCode;
use std::collections::HashMap;
use std::io::Read;

pub fn auth(username: &str, password: &str, channel: Channel) -> Result<(), reqwest::Error> {
    let client = reqwest::blocking::Client::builder()
                                .redirect(Policy::limited(16))
                                .user_agent("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/96.0.1043.1 Safari/537.36 Edg/96.0.1043.1")
                                .cookie_store(true)
                                .build().expect("Failed creating http client?");

    let mut res = client.get("https://i.njtech.edu.cn").send()?;
    let mut body = String::new();
    res.read_to_string(&mut body)
        .expect("failed reading response body");

    println!("Status: {}", res.status());
    println!("Headers:\n{:#?}", res.headers());

    // 表单提交url参数中有一个 clientId 是服务器生成的
    // 所以要分析表单 拿到 action 地址
    // 还有一个  name = "lt" 的 hidden input  也是随机生成的
    // 还有一个 name = "execution" 也是 随机的？
    let action_url_pattern = RegexBuilder::new(r#"<form.*action="(/cas/login.+?)""#)
        .multi_line(true)
        .build()
        .unwrap();
    let lt_input_pattern = RegexBuilder::new(r#"<input.*name\w?=\w?"lt" value="(.*?)""#)
        .multi_line(true)
        .build()
        .unwrap();
    let execution_input_pattern =
        RegexBuilder::new(r#"<input[\w\s\S]*name\w?=\w?"execution" value="(.*?)""#)
            .multi_line(true)
            .build()
            .unwrap();
    println!(
        "matching form url using regex pattern {}",
        action_url_pattern.as_str()
    );

    let mut form_submit_url = String::from("https://u.njtech.edu.cn").to_owned();
    let mut lt_input_value = String::new();
    let mut execution_input_value = String::new();

    for cap in action_url_pattern.captures_iter(&body) {
        form_submit_url.push_str(&cap[1]);
        println!(
            "Matched form submit url: {}, now posting..",
            form_submit_url
        );
    }
    for cap in lt_input_pattern.captures_iter(&body) {
        lt_input_value = cap[1].to_string();
    }
    for cap in execution_input_pattern.captures_iter(&body) {
        execution_input_value = cap[1].to_string();
    }

    //init parameters
    let mut params = HashMap::new();
    params.insert("username", username);
    params.insert("password", password);
    params.insert("channelshow", channel.to_channelshow_param());
    params.insert("channel", channel.to_channel_param());
    params.insert("lt", &lt_input_value);
    params.insert("execution", &execution_input_value);
    params.insert("_eventId", "submit");
    params.insert("login", "登录");

    println!("try posting with parameters:\n {:#?}", params);

    let post_resp = client.post(form_submit_url).form(&params).send().unwrap();
    if post_resp.status() == StatusCode::OK
        && post_resp.url().to_string() == "https://i.njtech.edu.cn/index.html"
    {
        println!("登录成功! \n{}\n{}", post_resp.status(), post_resp.url())
    } else {
        println!(
            "错误！\nStatus: {}\nHeaders:\n{:#?}",
            post_resp.status(),
            post_resp.headers()
        )
    }
    Ok(())
}

pub enum Channel {
    Default,
    Cmcc,
    Telecom,
}
impl Channel {
    pub fn to_channel_param(&self) -> &str {
        match self {
            Channel::Default => "default",
            Channel::Cmcc => "@cmcc",
            Channel::Telecom => "@telecom",
        }
    }

    pub fn to_channelshow_param(&self) -> &str {
        match self {
            Channel::Default => "校园内网",
            Channel::Cmcc => "中国移动",
            Channel::Telecom => "中国电信",
        }
    }
}
