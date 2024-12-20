const urlParams = new URLSearchParams(window.location.search);
//通过url获得当前主题
const theme = urlParams.get('theme');
const token = localStorage.getItem('jwt_token');

// 为调整大小的控制柄（通常出现在可调整大小的元素如文本框、窗口等的边缘或角落）添加事件监听器
document.querySelectorAll('.resizable-textarea::after').forEach(handle => {
    console.log('Resize handle clicked (but not implemented)', handle);
});

//开始自动执行的方法
window.onload = function () {
    addCleanButtonEvent(); //绑定清理按钮
    document.getElementById("themeTitle").innerText = `主题：${theme}`
}


//点击“清空”
function addCleanButtonEvent() {
    // 获取清空按钮
    var deleteButton = document.getElementById('delete');
    // 获取文本区域
    var textarea = document.querySelector('.resizable-textarea');

    // 给清空按钮添加点击事件监听器
    deleteButton.addEventListener('click', function () {
        var userConfirmed = confirm('是否清空灵感内容？');
        if (userConfirmed) {
            // 用户点击了“确定”，清空文本区域的内容
            textarea.value = '';
        } else {
            // 用户点击了“取消”，不执行任何操作
        }
    });
};
function addTag() {
    const tagContainer = document.getElementById("tagsContainer");
    // 创建一个新的子div
    const newTag = document.createElement('div');
    newTag.className = 'tag-item';
    newTag.contentEditable = "true";
    newTag.role = "textbox";
    newTag.innerText = "请输入标签";
    
    tagContainer.appendChild(newTag);
}

function deleteLastTag() {
    const tagContainer = document.getElementById("tagsContainer");
    const lastTag = tagContainer.lastElementChild;  // 获取最后一个子元素
    if (lastTag) {
        tagContainer.removeChild(lastTag);  // 删除最后一个子元素
    }
}
//——————————————————————————————————————————————
//保存标签
function saveTitle(input, parentElement) {
    const tagValue = parentElement.querySelector('.tag-value');
    const placeholderText = '请输入tag';

    if (input.value.trim()) {
        tagValue.textContent = input.value;
        tagValue.style.cursor = 'default';
    }
    else {
        parentElement.remove();
    }
    input.style.display = 'none'; // 隐藏输入框
}


//提交tag
function submitTag(tagName) {
    fetch('https://localhost:7050/api/ideas/tags', {
        method: 'POST',
        headers: {
            'Authorization': `Bearer ${token}`,
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({ tagName })
    })
        .then(response => response.json())
        .then(data => {
            console.log('Tags updated:', data);
            alert("成功！");
        })
        .catch(error => {
            console.error('Error:', error);
            alart('错误');
        });
}

function submitIdea() {
    // 获取网页元素构造请求体
    let title = document.getElementById("title").innerText;
    let description = document.getElementById("description").innerText;
    let postTheme = theme;

    const tagContainer = document.getElementById("tagsContainer");
    let tagItems = tagContainer.childNodes;

    // 初始化 tagsName 数组
    let tagsName = [];

    // 遍历 tagItems 并提取标签文本
    tagItems.forEach(function (element) {
        // 确保每个 element 是一个有效的 HTML 元素，并且其包含文本内容
        if (element.nodeType === 1 && element.innerText) { // nodeType 1 表示元素节点
            tagsName.push(element.innerText.trim()); // 获取标签文本并去除前后空白字符
        }
    });

    // 构建请求体
    let requestBody = {
        title: title,
        description: description,
        themeTitle: postTheme,
        tagsName: tagsName  // 使用从标签元素中提取的字符串数组
    };

    console.log("requestBody: " + JSON.stringify(requestBody));

    // 使用 fetch 发送 POST 请求
    fetch('/api/ideas', {
        method: 'POST',                         // 请求方法
        headers: {
            'Authorization': `Bearer ${token}`,
            'Content-Type': 'application/json',   // 指定请求体的格式为 JSON
        },
        body: JSON.stringify(requestBody)       // 将对象转换为 JSON 字符串
    })
        .then(response => response.json())      // 解析响应为 JSON
        .then(data => {
            console.log('Success:', data);        // 成功回调
        })
        .catch((error) => {
            console.error('Error:', error);       // 错误回调
        });
}



/*大标题的修改*/
function toggleEditHeader(header) {
    const span = header.querySelector('#title');
    const input = header.querySelector('#Input');
    if (input.style.display === 'none') {
        // 切换为编辑模式
        span.style.display = 'none';
        input.style.display = 'inline-block';
        input.value = span.textContent; // 设置输入框的初始值为标题的当前文本
        input.focus(); // 使输入框获得焦点
        input.select(); // 选中输入框内的所有文本（可选）
    } else {
        // 保存并退出编辑模式
        saveHeaderTitle(input);
    }
}

function saveHeaderTitle(input) {
    const span = input.parentElement.querySelector('#title');
    const value = input.value.trim(); // 去除首尾空格
    if (value !== '') {
        // 如果输入框不为空，则更新标题并隐藏输入框
        span.textContent = value;
    } else {
        // 如果输入框为空，则恢复为默认标题
        span.textContent = '请输入标题';
    }
    input.style.display = 'none'; // 隐藏输入框
    span.style.display = 'inline-block'; // 显示标题
}

function addSaveButtonEvent() {
    submitIdea();
    alert('保存成功！');
}
