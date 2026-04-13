```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method              | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------- |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Email_Partial       | Job-YFEFPZ | 10             | Default     | 3           | 103.12 ns |  1.357 ns |  0.897 ns |  1.00 |    0.01 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | Job-YFEFPZ | 10             | Default     | 3           | 440.84 ns | 19.405 ns | 12.835 ns |  4.28 |    0.12 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | Job-YFEFPZ | 10             | Default     | 3           | 503.79 ns | 21.702 ns | 14.354 ns |  4.89 |    0.14 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | Job-YFEFPZ | 10             | Default     | 3           | 418.11 ns | 25.755 ns | 17.036 ns |  4.05 |    0.16 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | Job-YFEFPZ | 10             | Default     | 3           | 187.34 ns | 14.601 ns |  9.657 ns |  1.82 |    0.09 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | Job-YFEFPZ | 10             | Default     | 3           | 128.97 ns | 10.747 ns |  7.108 ns |  1.25 |    0.07 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | Job-YFEFPZ | 10             | Default     | 3           | 232.53 ns | 19.395 ns | 12.829 ns |  2.26 |    0.12 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | Job-YFEFPZ | 10             | Default     | 3           | 198.37 ns | 10.794 ns |  7.139 ns |  1.92 |    0.07 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | Job-YFEFPZ | 10             | Default     | 3           |  57.43 ns |  1.052 ns |  0.696 ns |  0.56 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Short         | Job-YFEFPZ | 10             | Default     | 3           |  70.09 ns |  0.878 ns |  0.581 ns |  0.68 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Long          | Job-YFEFPZ | 10             | Default     | 3           | 102.54 ns |  1.014 ns |  0.671 ns |  0.99 |    0.01 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | Job-YFEFPZ | 10             | Default     | 3           | 586.74 ns |  3.753 ns |  2.482 ns |  5.69 |    0.05 | 0.0248 |     416 B |        1.86 |
|                     |            |                |             |             |           |           |           |       |         |        |           |             |
| Email_Partial       | MediumRun  | 15             | 2           | 10          |  98.09 ns |  1.253 ns |  1.875 ns |  1.00 |    0.03 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | MediumRun  | 15             | 2           | 10          | 423.36 ns |  1.929 ns |  2.704 ns |  4.32 |    0.09 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | MediumRun  | 15             | 2           | 10          | 490.52 ns |  4.459 ns |  6.395 ns |  5.00 |    0.11 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | MediumRun  | 15             | 2           | 10          | 417.61 ns |  3.047 ns |  4.560 ns |  4.26 |    0.09 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | MediumRun  | 15             | 2           | 10          | 188.44 ns |  1.441 ns |  2.157 ns |  1.92 |    0.04 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | MediumRun  | 15             | 2           | 10          | 124.02 ns |  1.214 ns |  1.702 ns |  1.26 |    0.03 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | MediumRun  | 15             | 2           | 10          | 226.57 ns |  1.924 ns |  2.821 ns |  2.31 |    0.05 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | MediumRun  | 15             | 2           | 10          | 182.63 ns |  1.102 ns |  1.580 ns |  1.86 |    0.04 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | MediumRun  | 15             | 2           | 10          |  54.21 ns |  0.224 ns |  0.314 ns |  0.55 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Short         | MediumRun  | 15             | 2           | 10          |  69.96 ns |  0.227 ns |  0.325 ns |  0.71 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Long          | MediumRun  | 15             | 2           | 10          | 103.23 ns |  2.031 ns |  3.039 ns |  1.05 |    0.04 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | MediumRun  | 15             | 2           | 10          | 591.83 ns |  1.140 ns |  1.707 ns |  6.04 |    0.12 | 0.0248 |     416 B |        1.86 |
