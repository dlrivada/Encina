```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method              | Job        | IterationCount | LaunchCount | Mean      | Error        | StdDev     | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------- |----------- |--------------- |------------ |----------:|-------------:|-----------:|------:|--------:|-------:|----------:|------------:|
| Email_Partial       | Job-YFEFPZ | 10             | Default     |  97.90 ns |     1.102 ns |   0.729 ns |  1.00 |    0.01 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | Job-YFEFPZ | 10             | Default     | 411.76 ns |     5.281 ns |   3.493 ns |  4.21 |    0.05 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | Job-YFEFPZ | 10             | Default     | 497.60 ns |     2.365 ns |   1.407 ns |  5.08 |    0.04 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | Job-YFEFPZ | 10             | Default     | 425.03 ns |    25.792 ns |  17.060 ns |  4.34 |    0.17 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | Job-YFEFPZ | 10             | Default     | 176.94 ns |     1.797 ns |   1.189 ns |  1.81 |    0.02 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | Job-YFEFPZ | 10             | Default     | 110.87 ns |     0.742 ns |   0.491 ns |  1.13 |    0.01 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | Job-YFEFPZ | 10             | Default     | 231.47 ns |     3.918 ns |   2.591 ns |  2.36 |    0.03 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | Job-YFEFPZ | 10             | Default     | 172.62 ns |     0.585 ns |   0.387 ns |  1.76 |    0.01 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | Job-YFEFPZ | 10             | Default     |  66.19 ns |     0.368 ns |   0.219 ns |  0.68 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Short         | Job-YFEFPZ | 10             | Default     |  71.97 ns |     0.303 ns |   0.201 ns |  0.74 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Long          | Job-YFEFPZ | 10             | Default     | 104.90 ns |     0.396 ns |   0.236 ns |  1.07 |    0.01 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | Job-YFEFPZ | 10             | Default     | 594.28 ns |     5.441 ns |   2.846 ns |  6.07 |    0.05 | 0.0229 |     392 B |        1.75 |
|                     |            |                |             |           |              |            |       |         |        |           |             |
| Email_Partial       | ShortRun   | 3              | 1           |  96.05 ns |     5.928 ns |   0.325 ns |  1.00 |    0.00 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | ShortRun   | 3              | 1           | 464.74 ns |   522.165 ns |  28.622 ns |  4.84 |    0.26 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | ShortRun   | 3              | 1           | 983.35 ns | 2,476.325 ns | 135.736 ns | 10.24 |    1.22 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | ShortRun   | 3              | 1           | 508.71 ns |   328.109 ns |  17.985 ns |  5.30 |    0.16 | 0.0305 |     520 B |        2.32 |
| Name_Partial        | ShortRun   | 3              | 1           | 295.01 ns |    62.107 ns |   3.404 ns |  3.07 |    0.03 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | ShortRun   | 3              | 1           | 126.93 ns |   126.910 ns |   6.956 ns |  1.32 |    0.06 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | ShortRun   | 3              | 1           | 253.05 ns |   252.409 ns |  13.835 ns |  2.63 |    0.12 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | ShortRun   | 3              | 1           | 195.33 ns |    93.180 ns |   5.108 ns |  2.03 |    0.05 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | ShortRun   | 3              | 1           |  70.09 ns |    45.239 ns |   2.480 ns |  0.73 |    0.02 | 0.0076 |     128 B |        0.57 |
| Email_Short         | ShortRun   | 3              | 1           |  71.74 ns |     4.133 ns |   0.227 ns |  0.75 |    0.00 | 0.0076 |     128 B |        0.57 |
| Email_Long          | ShortRun   | 3              | 1           | 105.43 ns |     3.554 ns |   0.195 ns |  1.10 |    0.00 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | ShortRun   | 3              | 1           | 704.70 ns | 1,846.450 ns | 101.210 ns |  7.34 |    0.91 | 0.0229 |     392 B |        1.75 |
