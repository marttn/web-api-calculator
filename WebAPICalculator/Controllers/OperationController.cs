using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NCalc;
using Newtonsoft.Json;

namespace WebAPICalculator.Controllers
{
    /// <summary>
    /// Operation Controller
    /// </summary>
    [Produces("application/json")]
    [ApiController]
    [Route("[controller]")]
    public class OperationController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="environment"></param>
        public OperationController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        /// <summary>
        /// Returns a calculator history
        /// </summary>
        /// <returns>History</returns>
        /// <response code="200">Returns result</response>
        [HttpGet]
        [Route("History")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IEnumerable<Operation> History()
        {
            return GetHistory().Result;
        }

        /// <summary>
        /// Searches in a calculator history
        /// </summary>
        /// <returns>History</returns>
        /// <param name="expression"></param>
        /// <returns>Result</returns>
        /// <response code="200">Returns result</response>
        /// <response code="400">Returns 400 if the expression is incorrect</response>
        [HttpGet]
        [Route("History/Search")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult Search(string expression)
        {
            var regex = new Regex(@"\d+\s*\**\/*\+*\-*\^*");
            if (string.IsNullOrEmpty(expression) || !regex.IsMatch(expression))
                return BadRequest();
            var e = new Expression(expression);
            if (e.HasErrors())
                return BadRequest();
            return Ok(GetHistory().Result.Where(x => x.Expression == expression || x.Result == expression));
        }

        /// <summary>
        /// Calculates a mathematical expression.
        /// Here are the symbols that the calculator understands:  +, -, *, / 
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     POST /Operation?expression=1%2B2
        ///
        /// </remarks>
        /// <param name="expression"></param>
        /// <returns>Result</returns>
        /// <response code="201">Returns result</response>
        /// <response code="400">Returns 400 if the expression is incorrect</response>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<Operation>> Create(string expression)
        {
            var regex = new Regex(@"^\w\d+\s*\**\/*\+*\-*\^*");
            if (string.IsNullOrEmpty(expression) || !regex.IsMatch(expression))
                return BadRequest();
            var e = new Expression(expression);
            if (e.HasErrors())
                return BadRequest();
            var operation = new Operation
            {
                Expression = expression,
                Result = e.Evaluate().ToString()
            };
            await WriteFile(operation);
            return operation;
        }

        private async Task<List<Operation>> GetHistory()
        {
            var contentRootPath = _environment.ContentRootPath + "\\data.json";
            using (var fileStream = new FileStream(contentRootPath, FileMode.OpenOrCreate, FileAccess.Read))
            {
                using (var reader = new StreamReader(fileStream))
                {
                    var jsonString = await reader.ReadToEndAsync();
                    var list = JsonConvert.DeserializeObject<List<Operation>>(jsonString);
                    return list;
                }
            }
        }
        private async Task WriteFile(Operation operation)
        {
            var contentRootPath = _environment.ContentRootPath + "\\data.json";
            var list = GetHistory().Result;
            using (var fileStream = new FileStream(contentRootPath, FileMode.Create, FileAccess.Write))
            {
                list.Add(operation);
                var convertedJson = JsonConvert.SerializeObject(list, Formatting.Indented);
                var bytes = Encoding.UTF8.GetBytes(convertedJson);
                await fileStream.WriteAsync(bytes, 0, bytes.Length);
            }
        }
    }
}
