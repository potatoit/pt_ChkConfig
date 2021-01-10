# pt_ChkConfig

This is a quick little program to parse the .zip file from an exported configuration from CMS005/CMS010/CMS015 etc.  It was hacked together pretty quickly due to having
a large number of CMS010 entries that I needed to import, many of them had sorting orders and it was painful when those sorting orders didn't exist.
It will look for sorting orders in the file.  It has the option of checking to see if the sorting order exists and can potentially create it (if the sorting order
isn't in use).  Note, it will not activate the sorting order.

Example usage:
pt_ChkConfig /file "C:\Temp\Config\ConfigData_PurchasePortal_TRANS ID LSTPOBYSUP_1.xml\TRANS ID LSTPOBYSUP2.zip" /ionapi "C:\Temp\Config\SAC-PostmanTest2.ionapi" /checksortorder /createsortingorder /pause

  /file <path to file or directory> - if a directory it will scan the files in that directory
  /ionapi <path to ionapi file> - this is the path to a valid .ionapi with a service account
  /checksortorder - this will check M3 if the .ionapi file is specified
  /createsortingorder - this will attempt to create the sorting order (will only do it if an existing sorting order doesn't exist, it will not activate the sorting order)
  /pause - if this argument is specified, it will pause the output before exiting the program

# Disclaimer
HE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
