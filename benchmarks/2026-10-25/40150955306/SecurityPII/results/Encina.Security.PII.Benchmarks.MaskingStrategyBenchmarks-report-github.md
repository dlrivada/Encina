```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4


```
| Method              | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error    | StdDev   | Median    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------- |----------- |--------------- |------------ |------------ |----------:|---------:|---------:|----------:|------:|--------:|-------:|----------:|------------:|
| Email_Partial       | Job-YFEFPZ | 10             | Default     | 3           |  74.84 ns | 1.090 ns | 0.649 ns |  74.56 ns |  1.00 |    0.01 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | Job-YFEFPZ | 10             | Default     | 3           | 327.12 ns | 4.534 ns | 2.698 ns | 325.66 ns |  4.37 |    0.05 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | Job-YFEFPZ | 10             | Default     | 3           | 394.99 ns | 3.138 ns | 2.075 ns | 395.45 ns |  5.28 |    0.05 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | Job-YFEFPZ | 10             | Default     | 3           | 319.59 ns | 7.319 ns | 4.841 ns | 317.44 ns |  4.27 |    0.07 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | Job-YFEFPZ | 10             | Default     | 3           | 138.27 ns | 0.744 ns | 0.443 ns | 138.22 ns |  1.85 |    0.02 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | Job-YFEFPZ | 10             | Default     | 3           |  86.77 ns | 0.624 ns | 0.413 ns |  86.75 ns |  1.16 |    0.01 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | Job-YFEFPZ | 10             | Default     | 3           | 180.77 ns | 1.869 ns | 1.236 ns | 180.95 ns |  2.42 |    0.03 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | Job-YFEFPZ | 10             | Default     | 3           | 135.88 ns | 1.550 ns | 1.025 ns | 135.73 ns |  1.82 |    0.02 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | Job-YFEFPZ | 10             | Default     | 3           |  51.31 ns | 0.169 ns | 0.112 ns |  51.31 ns |  0.69 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Short         | Job-YFEFPZ | 10             | Default     | 3           |  57.42 ns | 0.370 ns | 0.220 ns |  57.34 ns |  0.77 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Long          | Job-YFEFPZ | 10             | Default     | 3           |  85.40 ns | 0.529 ns | 0.350 ns |  85.44 ns |  1.14 |    0.01 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | Job-YFEFPZ | 10             | Default     | 3           | 492.65 ns | 3.771 ns | 2.495 ns | 493.49 ns |  6.58 |    0.06 | 0.0229 |     392 B |        1.75 |
|                     |            |                |             |             |           |          |          |           |       |         |        |           |             |
| Email_Partial       | MediumRun  | 15             | 2           | 10          |  77.96 ns | 2.043 ns | 2.864 ns |  77.95 ns |  1.00 |    0.05 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | MediumRun  | 15             | 2           | 10          | 326.00 ns | 1.883 ns | 2.701 ns | 326.48 ns |  4.19 |    0.16 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | MediumRun  | 15             | 2           | 10          | 400.61 ns | 3.555 ns | 5.320 ns | 399.97 ns |  5.15 |    0.20 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | MediumRun  | 15             | 2           | 10          | 326.74 ns | 6.698 ns | 9.169 ns | 327.65 ns |  4.20 |    0.19 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | MediumRun  | 15             | 2           | 10          | 147.09 ns | 4.662 ns | 6.686 ns | 152.50 ns |  1.89 |    0.11 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | MediumRun  | 15             | 2           | 10          |  89.90 ns | 0.878 ns | 1.315 ns |  89.91 ns |  1.15 |    0.04 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | MediumRun  | 15             | 2           | 10          | 180.94 ns | 1.741 ns | 2.497 ns | 180.66 ns |  2.32 |    0.09 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | MediumRun  | 15             | 2           | 10          | 146.71 ns | 1.974 ns | 2.954 ns | 147.35 ns |  1.88 |    0.08 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | MediumRun  | 15             | 2           | 10          |  49.34 ns | 0.123 ns | 0.169 ns |  49.32 ns |  0.63 |    0.02 | 0.0076 |     128 B |        0.57 |
| Email_Short         | MediumRun  | 15             | 2           | 10          |  57.59 ns | 1.472 ns | 2.158 ns |  58.49 ns |  0.74 |    0.04 | 0.0076 |     128 B |        0.57 |
| Email_Long          | MediumRun  | 15             | 2           | 10          |  81.53 ns | 0.648 ns | 0.970 ns |  81.72 ns |  1.05 |    0.04 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | MediumRun  | 15             | 2           | 10          | 497.64 ns | 0.867 ns | 1.216 ns | 497.53 ns |  6.39 |    0.23 | 0.0229 |     392 B |        1.75 |
