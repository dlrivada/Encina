```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method              | Job        | IterationCount | LaunchCount | Mean      | Error      | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------- |----------- |--------------- |------------ |----------:|-----------:|---------:|------:|--------:|-------:|----------:|------------:|
| Email_Partial       | Job-YFEFPZ | 10             | Default     | 106.76 ns |   1.146 ns | 0.600 ns |  1.00 |    0.01 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | Job-YFEFPZ | 10             | Default     | 420.68 ns |   6.416 ns | 4.244 ns |  3.94 |    0.04 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | Job-YFEFPZ | 10             | Default     | 485.21 ns |   7.721 ns | 5.107 ns |  4.55 |    0.05 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | Job-YFEFPZ | 10             | Default     | 409.80 ns |   3.516 ns | 2.325 ns |  3.84 |    0.03 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | Job-YFEFPZ | 10             | Default     | 190.28 ns |   2.033 ns | 1.345 ns |  1.78 |    0.02 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | Job-YFEFPZ | 10             | Default     | 126.30 ns |   2.471 ns | 1.635 ns |  1.18 |    0.02 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | Job-YFEFPZ | 10             | Default     | 236.91 ns |   3.159 ns | 2.089 ns |  2.22 |    0.02 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | Job-YFEFPZ | 10             | Default     | 189.93 ns |   3.495 ns | 2.312 ns |  1.78 |    0.02 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | Job-YFEFPZ | 10             | Default     |  63.75 ns |   1.484 ns | 0.982 ns |  0.60 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Short         | Job-YFEFPZ | 10             | Default     |  80.59 ns |   0.496 ns | 0.328 ns |  0.75 |    0.00 | 0.0076 |     128 B |        0.57 |
| Email_Long          | Job-YFEFPZ | 10             | Default     | 126.02 ns |   1.153 ns | 0.762 ns |  1.18 |    0.01 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | Job-YFEFPZ | 10             | Default     | 639.14 ns |   2.221 ns | 1.469 ns |  5.99 |    0.03 | 0.0229 |     392 B |        1.75 |
|                     |            |                |             |           |            |          |       |         |        |           |             |
| Email_Partial       | ShortRun   | 3              | 1           | 106.48 ns |  32.449 ns | 1.779 ns |  1.00 |    0.02 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | ShortRun   | 3              | 1           | 427.86 ns |  40.948 ns | 2.244 ns |  4.02 |    0.06 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | ShortRun   | 3              | 1           | 506.32 ns |  68.195 ns | 3.738 ns |  4.76 |    0.08 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | ShortRun   | 3              | 1           | 416.68 ns |  42.529 ns | 2.331 ns |  3.91 |    0.06 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | ShortRun   | 3              | 1           | 191.86 ns |  25.258 ns | 1.384 ns |  1.80 |    0.03 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | ShortRun   | 3              | 1           | 128.17 ns |  21.910 ns | 1.201 ns |  1.20 |    0.02 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | ShortRun   | 3              | 1           | 236.08 ns |  29.635 ns | 1.624 ns |  2.22 |    0.03 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | ShortRun   | 3              | 1           | 195.43 ns |  50.210 ns | 2.752 ns |  1.84 |    0.03 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | ShortRun   | 3              | 1           |  65.39 ns |   7.137 ns | 0.391 ns |  0.61 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Short         | ShortRun   | 3              | 1           |  76.33 ns |   1.809 ns | 0.099 ns |  0.72 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Long          | ShortRun   | 3              | 1           | 115.61 ns |   3.089 ns | 0.169 ns |  1.09 |    0.02 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | ShortRun   | 3              | 1           | 643.12 ns | 129.977 ns | 7.124 ns |  6.04 |    0.11 | 0.0229 |     392 B |        1.75 |
