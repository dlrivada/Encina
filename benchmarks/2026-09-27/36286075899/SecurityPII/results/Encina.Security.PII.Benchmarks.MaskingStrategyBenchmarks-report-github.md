```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method              | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error    | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------- |----------- |--------------- |------------ |------------ |----------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
| Email_Partial       | Job-YFEFPZ | 10             | Default     | 3           |  96.45 ns | 0.615 ns | 0.366 ns |  1.00 |    0.01 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | Job-YFEFPZ | 10             | Default     | 3           | 404.62 ns | 1.154 ns | 0.687 ns |  4.20 |    0.02 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | Job-YFEFPZ | 10             | Default     | 3           | 496.01 ns | 4.529 ns | 2.995 ns |  5.14 |    0.03 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | Job-YFEFPZ | 10             | Default     | 3           | 398.41 ns | 2.312 ns | 1.529 ns |  4.13 |    0.02 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | Job-YFEFPZ | 10             | Default     | 3           | 180.22 ns | 0.713 ns | 0.472 ns |  1.87 |    0.01 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | Job-YFEFPZ | 10             | Default     | 3           | 119.56 ns | 0.541 ns | 0.322 ns |  1.24 |    0.01 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | Job-YFEFPZ | 10             | Default     | 3           | 240.71 ns | 2.998 ns | 1.983 ns |  2.50 |    0.02 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | Job-YFEFPZ | 10             | Default     | 3           | 181.26 ns | 0.589 ns | 0.308 ns |  1.88 |    0.01 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | Job-YFEFPZ | 10             | Default     | 3           |  56.93 ns | 0.336 ns | 0.176 ns |  0.59 |    0.00 | 0.0076 |     128 B |        0.57 |
| Email_Short         | Job-YFEFPZ | 10             | Default     | 3           |  70.62 ns | 0.253 ns | 0.168 ns |  0.73 |    0.00 | 0.0076 |     128 B |        0.57 |
| Email_Long          | Job-YFEFPZ | 10             | Default     | 3           | 105.70 ns | 1.229 ns | 0.813 ns |  1.10 |    0.01 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | Job-YFEFPZ | 10             | Default     | 3           | 598.48 ns | 4.066 ns | 2.689 ns |  6.21 |    0.03 | 0.0248 |     416 B |        1.86 |
|                     |            |                |             |             |           |          |          |       |         |        |           |             |
| Email_Partial       | MediumRun  | 15             | 2           | 10          | 100.43 ns | 2.067 ns | 3.094 ns |  1.00 |    0.04 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | MediumRun  | 15             | 2           | 10          | 416.55 ns | 3.843 ns | 5.752 ns |  4.15 |    0.14 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | MediumRun  | 15             | 2           | 10          | 511.03 ns | 4.612 ns | 6.903 ns |  5.09 |    0.17 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | MediumRun  | 15             | 2           | 10          | 412.22 ns | 4.009 ns | 6.001 ns |  4.11 |    0.14 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | MediumRun  | 15             | 2           | 10          | 182.91 ns | 2.009 ns | 3.007 ns |  1.82 |    0.06 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | MediumRun  | 15             | 2           | 10          | 122.95 ns | 2.602 ns | 3.894 ns |  1.23 |    0.05 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | MediumRun  | 15             | 2           | 10          | 226.90 ns | 2.356 ns | 3.526 ns |  2.26 |    0.08 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | MediumRun  | 15             | 2           | 10          | 179.32 ns | 1.956 ns | 2.927 ns |  1.79 |    0.06 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | MediumRun  | 15             | 2           | 10          |  56.20 ns | 1.068 ns | 1.598 ns |  0.56 |    0.02 | 0.0076 |     128 B |        0.57 |
| Email_Short         | MediumRun  | 15             | 2           | 10          |  68.31 ns | 0.820 ns | 1.227 ns |  0.68 |    0.02 | 0.0076 |     128 B |        0.57 |
| Email_Long          | MediumRun  | 15             | 2           | 10          | 104.43 ns | 1.616 ns | 2.418 ns |  1.04 |    0.04 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | MediumRun  | 15             | 2           | 10          | 589.11 ns | 1.583 ns | 2.320 ns |  5.87 |    0.18 | 0.0248 |     416 B |        1.86 |
