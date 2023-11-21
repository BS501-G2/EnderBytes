<div class="favorites" style="width: 850px;">
  <div>
  <form class="search" style="display: flex; justify-content: flex-start; align-items: flex-start; margin-left: 150px; padding-top: 50px;">   
      <label for="default-search" class="mb-2 text-sm font-medium text-gray-900 sr-only dark:text-white">Search</label>
      <div class="relative">
          <div class="absolute inset-y-0 left-0 flex items-center pl-3 pointer-events-none">
              <svg class="w-4 h-4 text-gray-500 dark:text-gray-400" aria-hidden="true" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 20 20">
                  <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="m19 19-4-4m0-7A7 7 0 1 1 1 8a7 7 0 0 1 14 0Z"/>
              </svg>
          </div>
          <input type="search" id="default-search" style="width: 850px;" class="block w-full p-4 pl-10 text-sm text-gray-100 border border-gray-300 rounded-lg bg-gray-50 focus:ring-blue-500 focus:border-blue-500 dark:bg-gray-300 dark:border-gray-200 dark:placeholder-gray-100 dark:text-white dark:focus:ring-blue-500 dark:focus:border-blue-500" placeholder="Search your favorite files" required>
          <button type="submit" class="text-white absolute right-2.5 bottom-2.5 bg-blue-700 hover:bg-blue-800 focus:ring-4 focus:outline-none focus:ring-blue-300 font-medium rounded-lg text-sm px-4 py-2 dark:bg-blue-600 dark:hover:bg-blue-700 dark:focus:ring-blue-800">Search</button>
      </div>
  </form>
  </div>
  <div class="Categories" style="margin-left: 150px; padding-top: 30px;">
      <h1 class="Title" style="color: black; font-size:larger; font-weight: bold;">Favorites</h1>
  </div>

</div>
<div class="content" style="display: flex; justify-content: space-between; align-items: center;">
    <div class="favorite-files" style="margin-left: 150px; padding-left: 20px; padding-top: 10px; border: solid; border-color: var(--color-2); border-radius: 15px; height: 800px; width: 850px; overflow-x: hidden; overflow-y:auto   ;">
        <div class="file" style="display: flex;justify-content: space-between;align-items: center;padding: 10px;border: solid var(--color-2);height: 50px;border-radius: 10px;margin-bottom: 10px;margin-right: 10px;color: black; text-overflow:ellipsis; ">
            <div class="fileIcon"><img src="/images/fileIcon/docx-icon.png" width="20px"></div>
            <div class="fileName" style="max-width:300px;"><p style="font-weight: bold; width: 300px; overflow: hidden; display: inline-block; text-overflow: ellipsis; white-space: nowrap;">computer programming laboratory exercise</p></div>
            <div class="fileSize"><p style="color: gray;">*.filetype</p></div>  
            <div class="fileSize"><p style="color: gray;">*file size</p></div>
            <div class="createDate"><p style="color: gray;">*create date</p></div>
            <div class="dots"><img src="/images/fileIcon/ellipses-icon.png" width="20px"></div> 
        </div>

        <div class="file" style="display: flex;justify-content: space-between;align-items: center;padding: 10px;border: solid var(--color-2);height: 50px;border-radius: 10px;margin-bottom: 10px;margin-right: 10px;color: black; text-overflow:ellipsis; ">
            <div class="fileIcon"><img src="/images/fileIcon/docx-icon.png" width="20px"></div>
            <div class="fileName" style="max-width:300px;"><p style="font-weight: bold; width: 300px; overflow: hidden; display: inline-block; text-overflow: ellipsis; white-space: nowrap;">computer programming laboratory exercise</p></div>
            <div class="fileType"><p style="color: gray;">*.filetype</p></div>  
            <div class="fileSize"><p style="color: gray;">*file size</p></div>
            <div class="createDate"><p style="color: gray;">*create date</p></div>
            <div class="dots"><img src="/images/fileIcon/ellipses-icon.png" width="20px"></div>
        </div>

        <div class="file" style="display: flex;justify-content: space-between;align-items: center;padding: 10px;border: solid var(--color-2);height: 50px;border-radius: 10px;margin-bottom: 10px;margin-right: 10px;color: black; text-overflow:ellipsis; ">
            <div class="fileIcon"><img src="/images/fileIcon/docx-icon.png" width="20px"></div>
            <div class="fileName" style="max-width:300px;"><p style="font-weight: bold; width: 300px; overflow: hidden; display: inline-block; text-overflow: ellipsis; white-space: nowrap;">computer programming laboratory exercise</p></div>
            <div class="fileSize"><p style="color: gray;">*.filetype</p></div>  
            <div class="fileSize"><p style="color: gray;">*file size</p></div>
            <div class="createDate"><p style="color: gray;">*create date</p></div>
            <div class="dots"><img src="/images/fileIcon/ellipses-icon.png" width="20px"></div> 
        </div>


    </div>
    <div class="side-panel" style="display: flex;padding-top: 10px;border: solid var(--color-2);border-radius: 15px;height: 800px;width: 400px;margin-right: 60px;overflow: hidden auto;flex-direction: column;justify-content: flex-start;align-items: center;">
        <div class="filePreview"><img src="https://us-tuna-sounds-images.voicemod.net/86cfe336-0dee-4034-9a90-0be04870aea3-1692697337457.jfif" width="300px"></div>
        <div class="fileName-side-panel"><p style="color:black; font-weight:bolder; font-size:xx-large; padding-top: 10px;">*Filename</p></div>
        <div class="fileType-side-panel"><p style="color:gray; font-size: larger; padding-top: 10px;">*.filetype</p></div>
        <div class="fileSize-side-panel"><p style="color:gray; font-size: larger; padding-top: 10px;">*filesize</p></div>
        <div class="createDate-side-panel"><p style="color:gray; font-size: larger; padding-top: 10px;">*createdate</p></div>
    </div>
</div>