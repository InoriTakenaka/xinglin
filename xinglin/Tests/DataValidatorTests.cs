using Microsoft.VisualStudio.TestTools.UnitTesting;
using xinglin.Services.Data;

namespace xinglin.Tests
{
    [TestClass]
    public class DataValidatorTests
    {
        [TestMethod]
        public void IsRequired_ShouldReturnFalse_WhenValueIsNull()
        {
            // Arrange & Act
            var result = DataValidator.IsRequired(null);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void IsRequired_ShouldReturnFalse_WhenValueIsEmptyString()
        {
            // Arrange & Act
            var result = DataValidator.IsRequired(string.Empty);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void IsRequired_ShouldReturnFalse_WhenValueIsWhitespace()
        {
            // Arrange & Act
            var result = DataValidator.IsRequired("   ");

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void IsRequired_ShouldReturnTrue_WhenValueIsNotEmpty()
        {
            // Arrange & Act
            var result = DataValidator.IsRequired("test");

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void IsNumber_ShouldReturnFalse_WhenValueIsNull()
        {
            // Arrange & Act
            var result = DataValidator.IsNumber(null);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void IsNumber_ShouldReturnFalse_WhenValueIsNotNumber()
        {
            // Arrange & Act
            var result = DataValidator.IsNumber("test");

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void IsNumber_ShouldReturnTrue_WhenValueIsInteger()
        {
            // Arrange & Act
            var result = DataValidator.IsNumber("123");

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void IsNumber_ShouldReturnTrue_WhenValueIsDecimal()
        {
            // Arrange & Act
            var result = DataValidator.IsNumber("123.45");

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void IsEmail_ShouldReturnFalse_WhenValueIsNull()
        {
            // Arrange & Act
            var result = DataValidator.IsEmail(null);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void IsEmail_ShouldReturnFalse_WhenValueIsNotEmail()
        {
            // Arrange & Act
            var result = DataValidator.IsEmail("test");

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void IsEmail_ShouldReturnTrue_WhenValueIsValidEmail()
        {
            // Arrange & Act
            var result = DataValidator.IsEmail("test@example.com");

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void IsDate_ShouldReturnFalse_WhenValueIsNull()
        {
            // Arrange & Act
            var result = DataValidator.IsDate(null);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void IsDate_ShouldReturnFalse_WhenValueIsNotDate()
        {
            // Arrange & Act
            var result = DataValidator.IsDate("test");

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void IsDate_ShouldReturnTrue_WhenValueIsValidDate()
        {
            // Arrange & Act
            var result = DataValidator.IsDate("2023-12-31");

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void MinLength_ShouldReturnFalse_WhenValueIsNull()
        {
            // Arrange & Act
            var result = DataValidator.MinLength(null, 3);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void MinLength_ShouldReturnFalse_WhenValueLengthIsLessThanMin()
        {
            // Arrange & Act
            var result = DataValidator.MinLength("ab", 3);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void MinLength_ShouldReturnTrue_WhenValueLengthIsEqualToMin()
        {
            // Arrange & Act
            var result = DataValidator.MinLength("abc", 3);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void MinLength_ShouldReturnTrue_WhenValueLengthIsGreaterThanMin()
        {
            // Arrange & Act
            var result = DataValidator.MinLength("abcd", 3);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void MaxLength_ShouldReturnTrue_WhenValueIsNull()
        {
            // Arrange & Act
            var result = DataValidator.MaxLength(null, 3);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void MaxLength_ShouldReturnTrue_WhenValueLengthIsLessThanMax()
        {
            // Arrange & Act
            var result = DataValidator.MaxLength("ab", 3);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void MaxLength_ShouldReturnTrue_WhenValueLengthIsEqualToMax()
        {
            // Arrange & Act
            var result = DataValidator.MaxLength("abc", 3);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void MaxLength_ShouldReturnFalse_WhenValueLengthIsGreaterThanMax()
        {
            // Arrange & Act
            var result = DataValidator.MaxLength("abcd", 3);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void IsPhone_ShouldReturnFalse_WhenValueIsNull()
        {
            // Arrange & Act
            var result = DataValidator.IsPhone(null);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void IsPhone_ShouldReturnFalse_WhenValueIsNotPhone()
        {
            // Arrange & Act
            var result = DataValidator.IsPhone("12345");

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void IsPhone_ShouldReturnTrue_WhenValueIsValidPhone()
        {
            // Arrange & Act
            var result = DataValidator.IsPhone("13812345678");

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void IsIdCard_ShouldReturnFalse_WhenValueIsNull()
        {
            // Arrange & Act
            var result = DataValidator.IsIdCard(null);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void IsIdCard_ShouldReturnFalse_WhenValueIsNotIdCard()
        {
            // Arrange & Act
            var result = DataValidator.IsIdCard("123456");

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void Validate_ShouldReturnValid_WhenNoRulesViolated()
        {
            // Arrange
            var validator = new DataValidator();
            validator.AddRule("Name", DataValidator.IsRequired, "Name is required");
            var data = new { Name = "test" };

            // Act
            var result = validator.Validate(data);

            // Assert
            Assert.IsTrue(result.IsValid);
            Assert.AreEqual(0, result.Errors.Count);
        }

        [TestMethod]
        public void Validate_ShouldReturnInvalid_WhenRulesViolated()
        {
            // Arrange
            var validator = new DataValidator();
            validator.AddRule("Name", DataValidator.IsRequired, "Name is required");
            var data = new { Name = string.Empty };

            // Act
            var result = validator.Validate(data);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(1, result.Errors.Count);
            Assert.AreEqual("Name is required", result.Errors[0]);
        }
    }
}
