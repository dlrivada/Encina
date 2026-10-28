```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.99GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method              | Job        | IterationCount | LaunchCount | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------- |----------- |--------------- |------------ |----------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| Email_Partial       | Job-YFEFPZ | 10             | Default     | 109.37 ns |  0.601 ns | 0.398 ns |  1.00 |    0.00 | 0.0026 |     224 B |        1.00 |
| Phone_Partial       | Job-YFEFPZ | 10             | Default     | 431.19 ns |  1.602 ns | 0.954 ns |  3.94 |    0.02 | 0.0062 |     520 B |        2.32 |
| CreditCard_Partial  | Job-YFEFPZ | 10             | Default     | 511.41 ns |  2.017 ns | 1.200 ns |  4.68 |    0.02 | 0.0057 |     544 B |        2.43 |
| SSN_Partial         | Job-YFEFPZ | 10             | Default     | 423.37 ns |  0.661 ns | 0.437 ns |  3.87 |    0.01 | 0.0062 |     520 B |        2.32 |
| Name_Partial        | Job-YFEFPZ | 10             | Default     | 198.73 ns |  0.848 ns | 0.561 ns |  1.82 |    0.01 | 0.0033 |     280 B |        1.25 |
| Address_Partial     | Job-YFEFPZ | 10             | Default     | 124.51 ns |  0.739 ns | 0.489 ns |  1.14 |    0.01 | 0.0038 |     328 B |        1.46 |
| DateOfBirth_Partial | Job-YFEFPZ | 10             | Default     | 258.26 ns |  0.696 ns | 0.414 ns |  2.36 |    0.01 | 0.0043 |     384 B |        1.71 |
| IPAddress_Partial   | Job-YFEFPZ | 10             | Default     | 186.22 ns |  0.262 ns | 0.137 ns |  1.70 |    0.01 | 0.0031 |     264 B |        1.18 |
| Custom_FullMasking  | Job-YFEFPZ | 10             | Default     |  60.45 ns |  0.167 ns | 0.110 ns |  0.55 |    0.00 | 0.0014 |     128 B |        0.57 |
| Email_Short         | Job-YFEFPZ | 10             | Default     |  74.14 ns |  0.247 ns | 0.147 ns |  0.68 |    0.00 | 0.0014 |     128 B |        0.57 |
| Email_Long          | Job-YFEFPZ | 10             | Default     | 120.44 ns |  0.762 ns | 0.504 ns |  1.10 |    0.01 | 0.0038 |     328 B |        1.46 |
| RegexPattern        | Job-YFEFPZ | 10             | Default     | 589.30 ns |  5.198 ns | 3.093 ns |  5.39 |    0.03 | 0.0038 |     392 B |        1.75 |
|                     |            |                |             |           |           |          |       |         |        |           |             |
| Email_Partial       | ShortRun   | 3              | 1           | 109.21 ns |  3.895 ns | 0.214 ns |  1.00 |    0.00 | 0.0026 |     224 B |        1.00 |
| Phone_Partial       | ShortRun   | 3              | 1           | 450.00 ns | 18.368 ns | 1.007 ns |  4.12 |    0.01 | 0.0062 |     520 B |        2.32 |
| CreditCard_Partial  | ShortRun   | 3              | 1           | 518.24 ns | 12.347 ns | 0.677 ns |  4.75 |    0.01 | 0.0057 |     544 B |        2.43 |
| SSN_Partial         | ShortRun   | 3              | 1           | 426.50 ns |  8.585 ns | 0.471 ns |  3.91 |    0.01 | 0.0062 |     520 B |        2.32 |
| Name_Partial        | ShortRun   | 3              | 1           | 199.59 ns | 13.453 ns | 0.737 ns |  1.83 |    0.01 | 0.0033 |     280 B |        1.25 |
| Address_Partial     | ShortRun   | 3              | 1           | 132.14 ns |  3.082 ns | 0.169 ns |  1.21 |    0.00 | 0.0038 |     328 B |        1.46 |
| DateOfBirth_Partial | ShortRun   | 3              | 1           | 260.12 ns |  6.976 ns | 0.382 ns |  2.38 |    0.01 | 0.0043 |     384 B |        1.71 |
| IPAddress_Partial   | ShortRun   | 3              | 1           | 188.97 ns |  3.722 ns | 0.204 ns |  1.73 |    0.00 | 0.0031 |     264 B |        1.18 |
| Custom_FullMasking  | ShortRun   | 3              | 1           |  61.02 ns |  2.755 ns | 0.151 ns |  0.56 |    0.00 | 0.0014 |     128 B |        0.57 |
| Email_Short         | ShortRun   | 3              | 1           |  75.02 ns |  2.941 ns | 0.161 ns |  0.69 |    0.00 | 0.0014 |     128 B |        0.57 |
| Email_Long          | ShortRun   | 3              | 1           | 120.97 ns |  5.266 ns | 0.289 ns |  1.11 |    0.00 | 0.0038 |     328 B |        1.46 |
| RegexPattern        | ShortRun   | 3              | 1           | 595.88 ns |  6.026 ns | 0.330 ns |  5.46 |    0.01 | 0.0038 |     392 B |        1.75 |
