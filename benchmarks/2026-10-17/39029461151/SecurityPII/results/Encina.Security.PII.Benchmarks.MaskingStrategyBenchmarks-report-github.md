```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method              | Job        | IterationCount | LaunchCount | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------- |----------- |--------------- |------------ |----------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| Email_Partial       | Job-YFEFPZ | 10             | Default     |  80.97 ns |  0.902 ns | 0.597 ns |  1.00 |    0.01 | 0.0026 |     224 B |        1.00 |
| Phone_Partial       | Job-YFEFPZ | 10             | Default     | 338.78 ns |  2.958 ns | 1.957 ns |  4.18 |    0.04 | 0.0062 |     520 B |        2.32 |
| CreditCard_Partial  | Job-YFEFPZ | 10             | Default     | 404.71 ns |  1.922 ns | 1.271 ns |  5.00 |    0.04 | 0.0062 |     544 B |        2.43 |
| SSN_Partial         | Job-YFEFPZ | 10             | Default     | 327.43 ns |  2.233 ns | 1.329 ns |  4.04 |    0.03 | 0.0062 |     520 B |        2.32 |
| Name_Partial        | Job-YFEFPZ | 10             | Default     | 152.76 ns |  1.724 ns | 1.026 ns |  1.89 |    0.02 | 0.0033 |     280 B |        1.25 |
| Address_Partial     | Job-YFEFPZ | 10             | Default     |  89.33 ns |  1.004 ns | 0.664 ns |  1.10 |    0.01 | 0.0038 |     328 B |        1.46 |
| DateOfBirth_Partial | Job-YFEFPZ | 10             | Default     | 199.97 ns |  1.488 ns | 0.885 ns |  2.47 |    0.02 | 0.0045 |     384 B |        1.71 |
| IPAddress_Partial   | Job-YFEFPZ | 10             | Default     | 143.49 ns |  1.418 ns | 0.938 ns |  1.77 |    0.02 | 0.0031 |     264 B |        1.18 |
| Custom_FullMasking  | Job-YFEFPZ | 10             | Default     |  45.18 ns |  0.615 ns | 0.407 ns |  0.56 |    0.01 | 0.0015 |     128 B |        0.57 |
| Email_Short         | Job-YFEFPZ | 10             | Default     |  56.28 ns |  1.104 ns | 0.730 ns |  0.70 |    0.01 | 0.0014 |     128 B |        0.57 |
| Email_Long          | Job-YFEFPZ | 10             | Default     |  85.56 ns |  0.867 ns | 0.574 ns |  1.06 |    0.01 | 0.0038 |     328 B |        1.46 |
| RegexPattern        | Job-YFEFPZ | 10             | Default     | 477.06 ns |  4.364 ns | 2.597 ns |  5.89 |    0.05 | 0.0038 |     392 B |        1.75 |
|                     |            |                |             |           |           |          |       |         |        |           |             |
| Email_Partial       | ShortRun   | 3              | 1           |  80.98 ns |  1.484 ns | 0.081 ns |  1.00 |    0.00 | 0.0026 |     224 B |        1.00 |
| Phone_Partial       | ShortRun   | 3              | 1           | 343.71 ns | 11.987 ns | 0.657 ns |  4.24 |    0.01 | 0.0062 |     520 B |        2.32 |
| CreditCard_Partial  | ShortRun   | 3              | 1           | 409.12 ns | 85.058 ns | 4.662 ns |  5.05 |    0.05 | 0.0062 |     544 B |        2.43 |
| SSN_Partial         | ShortRun   | 3              | 1           | 335.88 ns | 36.722 ns | 2.013 ns |  4.15 |    0.02 | 0.0062 |     520 B |        2.32 |
| Name_Partial        | ShortRun   | 3              | 1           | 153.93 ns | 26.840 ns | 1.471 ns |  1.90 |    0.02 | 0.0033 |     280 B |        1.25 |
| Address_Partial     | ShortRun   | 3              | 1           |  95.67 ns | 23.350 ns | 1.280 ns |  1.18 |    0.01 | 0.0038 |     328 B |        1.46 |
| DateOfBirth_Partial | ShortRun   | 3              | 1           | 201.97 ns | 12.338 ns | 0.676 ns |  2.49 |    0.01 | 0.0045 |     384 B |        1.71 |
| IPAddress_Partial   | ShortRun   | 3              | 1           | 148.04 ns | 22.210 ns | 1.217 ns |  1.83 |    0.01 | 0.0031 |     264 B |        1.18 |
| Custom_FullMasking  | ShortRun   | 3              | 1           |  45.58 ns |  1.260 ns | 0.069 ns |  0.56 |    0.00 | 0.0015 |     128 B |        0.57 |
| Email_Short         | ShortRun   | 3              | 1           |  56.96 ns |  3.895 ns | 0.213 ns |  0.70 |    0.00 | 0.0014 |     128 B |        0.57 |
| Email_Long          | ShortRun   | 3              | 1           |  86.61 ns | 33.250 ns | 1.823 ns |  1.07 |    0.02 | 0.0038 |     328 B |        1.46 |
| RegexPattern        | ShortRun   | 3              | 1           | 485.25 ns | 40.339 ns | 2.211 ns |  5.99 |    0.02 | 0.0038 |     392 B |        1.75 |
