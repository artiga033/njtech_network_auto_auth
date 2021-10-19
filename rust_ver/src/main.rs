use njtech_home_autoauth::*;
fn main() {
    let username = "PLACEHOLDER_USERNAME";
    let password = "PLACEHOLDER_PASSWORD";
    let channel: Channel = Channel::Default;

    let result = auth(username, password, channel);
    if let Err(e) = result {
        println!("{:?}", e);
        println!("未知错误!");
    }
    std::thread::sleep(std::time::Duration::from_secs(3));
}
