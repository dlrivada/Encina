```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method              | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Median    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------- |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Email_Partial       | Job-YFEFPZ | 10             | Default     | 3           | 100.89 ns |  1.210 ns |  0.800 ns | 100.90 ns |  1.00 |    0.01 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | Job-YFEFPZ | 10             | Default     | 3           | 428.48 ns |  1.492 ns |  0.987 ns | 428.52 ns |  4.25 |    0.03 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | Job-YFEFPZ | 10             | Default     | 3           | 499.91 ns |  4.221 ns |  2.792 ns | 499.01 ns |  4.96 |    0.05 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | Job-YFEFPZ | 10             | Default     | 3           | 409.67 ns |  2.526 ns |  1.503 ns | 409.38 ns |  4.06 |    0.03 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | Job-YFEFPZ | 10             | Default     | 3           | 183.33 ns |  0.968 ns |  0.640 ns | 183.22 ns |  1.82 |    0.02 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | Job-YFEFPZ | 10             | Default     | 3           | 124.61 ns |  0.810 ns |  0.482 ns | 124.72 ns |  1.24 |    0.01 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | Job-YFEFPZ | 10             | Default     | 3           | 224.18 ns |  1.920 ns |  1.270 ns | 223.91 ns |  2.22 |    0.02 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | Job-YFEFPZ | 10             | Default     | 3           | 185.83 ns |  1.115 ns |  0.738 ns | 185.64 ns |  1.84 |    0.02 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | Job-YFEFPZ | 10             | Default     | 3           |  57.47 ns |  0.546 ns |  0.361 ns |  57.51 ns |  0.57 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Short         | Job-YFEFPZ | 10             | Default     | 3           |  72.13 ns |  0.824 ns |  0.545 ns |  72.27 ns |  0.72 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Long          | Job-YFEFPZ | 10             | Default     | 3           | 110.09 ns |  0.911 ns |  0.603 ns | 109.89 ns |  1.09 |    0.01 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | Job-YFEFPZ | 10             | Default     | 3           | 611.98 ns |  1.348 ns |  0.802 ns | 611.87 ns |  6.07 |    0.05 | 0.0248 |     416 B |        1.86 |
|                     |            |                |             |             |           |           |           |           |       |         |        |           |             |
| Email_Partial       | MediumRun  | 15             | 2           | 10          | 100.11 ns |  1.037 ns |  1.520 ns | 100.32 ns |  1.00 |    0.02 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | MediumRun  | 15             | 2           | 10          | 424.82 ns |  3.311 ns |  4.853 ns | 425.65 ns |  4.24 |    0.08 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | MediumRun  | 15             | 2           | 10          | 504.37 ns |  1.421 ns |  2.038 ns | 503.86 ns |  5.04 |    0.08 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | MediumRun  | 15             | 2           | 10          | 419.01 ns |  1.048 ns |  1.537 ns | 419.01 ns |  4.19 |    0.06 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | MediumRun  | 15             | 2           | 10          | 190.09 ns |  1.299 ns |  1.905 ns | 189.74 ns |  1.90 |    0.03 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | MediumRun  | 15             | 2           | 10          | 125.56 ns |  0.810 ns |  1.162 ns | 125.60 ns |  1.25 |    0.02 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | MediumRun  | 15             | 2           | 10          | 230.94 ns |  0.682 ns |  1.000 ns | 231.02 ns |  2.31 |    0.04 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | MediumRun  | 15             | 2           | 10          | 183.93 ns |  0.930 ns |  1.392 ns | 184.03 ns |  1.84 |    0.03 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | MediumRun  | 15             | 2           | 10          |  73.97 ns | 10.881 ns | 15.949 ns |  88.84 ns |  0.74 |    0.16 | 0.0076 |     128 B |        0.57 |
| Email_Short         | MediumRun  | 15             | 2           | 10          |  73.82 ns |  0.262 ns |  0.384 ns |  73.81 ns |  0.74 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Long          | MediumRun  | 15             | 2           | 10          | 108.50 ns |  0.626 ns |  0.917 ns | 108.26 ns |  1.08 |    0.02 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | MediumRun  | 15             | 2           | 10          | 594.47 ns |  1.695 ns |  2.485 ns | 594.08 ns |  5.94 |    0.09 | 0.0248 |     416 B |        1.86 |
