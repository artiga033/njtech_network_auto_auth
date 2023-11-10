# /bin/sh

###### 配置
account='' # 账号
isp='' # 运营商 中国电信->telecom 中国移动->cmcc 校园内网留空
password='' # 密码

timeout=10 #请求的超时时间
######

###### 常量区
UA='Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0 Safari/537.36 Edg/119.0.0.0'
######

test=$(curl -m $timeout http://www.msftconnecttest.com/connecttest.txt)
if [ "$test" == "Microsoft Connect Test" ]; then
echo 'online'
exit 0
fi
echo 'offline'
echo 'trying to login...'
# get the Location header value of the result
location=$(curl -m $timeout http://www.msftconnecttest.com/connecttest.txt -I -s | grep Location | awk '{print $2}')
if [ -z "$location" ]; then
echo 'failed to get location'
exit 1
fi
echo "redirected to $location"
# get wlanuserip
# example location string: http://10.50.255.12/a79.htm?wlanuserip=10.37.78.101&wlanacname=me60-2
# we need to get the wlanuserip query
wlanuserip=$(echo $location | awk -F '?' '{print $2}' | awk -F '&' '{print $1}' | awk -F '=' '{print $2}')
if [ -z "$wlanuserip" ]; then
echo 'failed to get wlanuserip'
exit 1
fi

echo "got your wlan_user_ip $wlanuserip"
echo "attempting to login..."
curl --url "http://10.50.255.12:801/eportal/portal/login?callback=dr1003&login_method=1&user_account=%2C0%2C$account%40$isp&user_password=$password&wlan_user_ip=$wlanuserip&wlan_user_ipv6=&wlan_user_mac=000000000000&wlan_ac_ip=&wlan_ac_name=&jsVersion=4.1.3&terminal_type=1&lang=zh-cn&lang=zh&v=5968" \
  -H "accept: */*" \
  -H "accept-language: zh-CN,zh;q=0.9,en;q=0.8" \
  -H "connection: keep-alive" \
  -H "host: 10.50.255.12:801" \
  -H "referer: http://10.50.255.12/" \
  -H "user-agent: $UA" \
  -m $timeout
