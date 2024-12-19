// 登录表单提交处理
document.getElementById('login-form').addEventListener('submit', function(event) {
    event.preventDefault();
  
    const email = document.getElementById('email').value;
    const password = document.getElementById('password').value;
  
    // 发送登录请求
    fetch('/api/login', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ email, password }), // 发送数据到后端
    })
    .then(response => response.json()) // 处理响应
    .then(data => {
      if (data.success) {
        alert('Login successful!');
        window.location.href = '/dashboard'; // 登录成功后跳转到主页面
      } else {
        alert('Login failed: ' + data.message); // 显示错误消息
      }
    })
    .catch(error => {
      console.error('Error:', error);
    });
  });
  