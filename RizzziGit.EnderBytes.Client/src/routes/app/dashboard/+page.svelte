<style lang="postcss">
  @media only screen and (max-width: 480px) {
    .dashboard{
      margin:0px;
    }
      .Categories{
        backound-color: blue;
        margin-grleft:0px;

      }
      .form{
        margin-left:25px;
      }
  }
</style>

<div class="dashboard">
  <div class="form" style="margin-left:10%; display: flex; justify-content: flex-start; align-items: flex-start; padding-top: 50px;">
  <form class="search">
      <label for="default-search" class="mb-2 text-sm font-medium text-gray-900 sr-only dark:text-white">Search</label>
      <div class="relative">
          <div class="absolute inset-y-0 left-0 flex items-center pl-3 pointer-events-none">
              <svg class="w-4 h-4 text-gray-500 dark:text-gray-400" aria-hidden="true" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 20 20">
                  <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="m19 19-4-4m0-7A7 7 0 1 1 1 8a7 7 0 0 1 14 0Z"/>
              </svg>
          </div>
          <div class="w-[750px]">
            <input type="search" id="default-search"class="block w-full p-4 pl-10 text-sm text-gray-100 border border-gray-300 rounded-lg bg-gray-50 focus:ring-blue-500 focus:border-blue-500 dark:bg-gray-300 dark:border-gray-200 dark:placeholder-gray-100 dark:text-white dark:focus:ring-blue-500 dark:focus:border-blue-500 w-full " placeholder="Search your files" required>
            <button type="submit" class="text-white absolute right-2.5 bottom-2.5 bg-blue-700 hover:bg-blue-800 focus:ring-4 focus:outline-none focus:ring-blue-300 font-medium rounded-lg text-sm px-4 py-2 dark:bg-blue-600 dark:hover:bg-blue-700 dark:focus:ring-blue-800">Search</button>
          </div>
      </div>
  </form>
  </div>

  <div class="Categories"  style="margin-left:10%; padding-top: 20px;">
      <h1 class="TitleCategories" style="color: #FFC107; font-size: 24px; font-semibold;">Categories</h1>
      <div class="button flex" style="padding-top: 20px;">
          <div style="padding-right: 20px;">
              <button class="border-2 border-blue-500 rounded-lg p-4" style="width: 120px; height: 120px;">
                  <h2 class="text-xl text-blue-500">Pictures</h2>
                  <p class="text-gray-500" style="padding-top: 20px;">380 Files</p>
               </button>
          </div>
          <div style="padding-right: 20px;">
              <button class="border-2 border-blue-500 rounded-lg p-4 " style="width: 120px; height: 120px; ">
                  <h2 class="text-xl text-blue-500">Documents</h2>
                  <p class="text-gray-500" style="padding-top: 20px;">180 Files</p>
              </button>
          </div>
          <div style="padding-right: 20px;">
              <button class="border-2 border-blue-500 rounded-lg p-4"style="width: 120px; height: 120px;">
                  <h2 class="text-xl text-blue-500">Videos</h2>
                  <p class="text-gray-500" style="padding-top: 20px;">120 Files</p>
              </button>
          </div>
          <div style="padding-right: 20px;">
              <button class="border-2 border-blue-500 rounded-lg p-4"style="width: 120px; height: 120px;">
                  <h2 class="text-xl text-blue-500">Audio</h2>
                  <p class="text-gray-500" style="padding-top: 20px;">50 Files</p>
              </button>
          </div>
      </div>
  </div>

  <div class="Files"  style="margin-left:10%; padding-top: 20px;">
      <h1 class="TitleFiles" style="color: #FFC107; font-size: 24px; font-semibold;">Files</h1>
      <div class="button flex" style="padding-top: 20px;">
          <div style="padding-right: 20px;">
              <button class="border-2 border-blue-500 rounded-lg p-4" style="width: 120px; height: 120px; padding: 10px;">
                  <h2 class="text-xl text-blue-500">Work</h2>
                  <p class="text-gray-500" style="padding-top: 20px;">380 Files</p>
               </button>
          </div>
          <div style="padding-right: 20px;">
              <button class="border-2 border-blue-500 rounded-lg p-4 " style="width: 120px; height: 120px; ">
                      <h2 class="text-xl text-blue-500">Personal</h2>
                      <p class="text-gray-500" style="padding-top: 20px;">180 Files</p>
              </button>
          </div>
          <div style="padding-right: 20px;">
              <button class="border-2 border-blue-500 rounded-lg p-4"style="width: 120px; height: 120px;">
                  <h2 class="text-xl text-blue-500">School</h2>
                  <p class="text-gray-500" style="padding-top: 20px;">120 Files</p>
              </button>
          </div>
          <div style="padding-right: 20px;">
              <button class="border-2 border-blue-500 rounded-lg p-4"style="width: 120px; height: 120px;">
                  <h2 class="text-xl text-blue-500">Archive</h2>
                  <p class="text-gray-500" style="padding-top: 20px;">50 Files</p>
              </button>
          </div>
          <div style="padding-right: 20px;">
              <button class="border-2 border-blue-500 rounded-lg p-4" style="width: 120px; height: 120px; display: flex; justify-content: center; align-items: center;">
                  <svg xmlns="http://www.w3.org/2000/svg" width="50" height="50" class="text-blue-500" viewBox="0 0 24 24" stroke="currentColor">
                      <path stroke-width="2" d="M12 5v14M5 12h14" fill="none" />
                  </svg>
              </button>
          </div>


      </div>

  </div>
  <div class="RecentFiles"  style="margin-left:10%; padding-top: 20px;">
      <h1 class="TitleRecentFiles" style="color: #FFC107; font-size: 24px; font-semibold; padding-bottom:20px">Recent Files</h1>
              <div class="file" style="display: flex;justify-content: space-between;align-items: center;padding: 10px;border: solid black;height: 50px;border-radius: 10px;margin-bottom: 10px;margin-right: 10px;color: black; text-overflow:ellipsis; width: 700px; ">
                  <div class="fileIcon"><img src="/images/fileIcon/docx-icon.png" width="20px"></div>
                  <div class="fileName" style="max-width:300px;"><p style="font-weight: bold; width: 300px; overflow: hidden; display: inline-block; text-overflow: ellipsis; white-space: nowrap;">computer programming laboratory exercise</p></div>
                  <div class="fileSize"><p style="color: gray;">*.filetype</p></div>
                  <div class="fileSize"><p style="color: gray;">*file size</p></div>
                  <div class="createDate"><p style="color: gray;">*create date</p></div>
                  <div class="dots"><img src="/images/fileIcon/ellipses-icon.png" width="20px"></div>
              </div>
              <div class="file" style="display: flex;justify-content: space-between;align-items: center;padding: 10px;border: solid black;height: 50px;border-radius: 10px;margin-bottom: 10px;margin-right: 10px;color: black; text-overflow:ellipsis; width: 700px; ">
                  <div class="fileIcon"><img src="/images/fileIcon/docx-icon.png" width="20px"></div>
                  <div class="fileName" style="max-width:300px;"><p style="font-weight: bold; width: 300px; overflow: hidden; display: inline-block; text-overflow: ellipsis; white-space: nowrap;">computer programming laboratory exercise</p></div>
                  <div class="fileSize"><p style="color: gray;">*.filetype</p></div>
                  <div class="fileSize"><p style="color: gray;">*file size</p></div>
                  <div class="createDate"><p style="color: gray;">*create date</p></div>
                  <div class="dots"><img src="/images/fileIcon/ellipses-icon.png" width="20px"></div>
              </div>
               <div class="file" style="display: flex;justify-content: space-between;align-items: center;padding: 10px;border: solid black;height: 50px;border-radius: 10px;margin-bottom: 10px;margin-right: 10px;color: black; text-overflow:ellipsis; width: 700px; ">
                  <div class="fileIcon"><img src="/images/fileIcon/docx-icon.png" width="20px"></div>
                  <div class="fileName" style="max-width:300px;"><p style="font-weight: bold; width: 300px; overflow: hidden; display: inline-block; text-overflow: ellipsis; white-space: nowrap;">computer programming laboratory exercise</p></div>
                  <div class="fileSize"><p style="color: gray;">*.filetype</p></div>
                  <div class="fileSize"><p style="color: gray;">*file size</p></div>
                  <div class="createDate"><p style="color: gray;">*create date</p></div>
                  <div class="dots"><img src="/images/fileIcon/ellipses-icon.png" width="20px"></div>
              </div>

  </div>


</div>
