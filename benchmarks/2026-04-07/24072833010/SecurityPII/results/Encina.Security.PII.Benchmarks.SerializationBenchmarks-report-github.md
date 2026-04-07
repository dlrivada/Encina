```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                   | Job        | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount | Mean            | Error    | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |------------ |------------- |------------ |----------------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
| Serialize_Small          | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        433.3 ns |  0.94 ns |  0.62 ns |  1.00 |    0.00 | 0.0067 |     112 B |        1.00 |
| Serialize_Medium         | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1,041.5 ns |  1.78 ns |  1.06 ns |  2.40 |    0.00 | 0.0229 |     392 B |        3.50 |
| Serialize_Large          | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      2,252.5 ns | 17.75 ns |  9.29 ns |  5.20 |    0.02 | 0.2251 |    3784 B |       33.79 |
| SerializeRoundtrip_Small | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        933.8 ns |  1.06 ns |  0.63 ns |  2.16 |    0.00 | 0.0134 |     232 B |        2.07 |
| SerializeRoundtrip_Large | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      5,116.3 ns | 58.11 ns | 34.58 ns | 11.81 |    0.08 | 0.4578 |    7688 B |       68.64 |
| ParseAndModify_Small     | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1,835.3 ns |  4.88 ns |  2.90 ns |  4.24 |    0.01 | 0.0496 |     840 B |        7.50 |
| MaskObject_Small         | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4,559.7 ns |  7.90 ns |  4.70 ns | 10.52 |    0.02 | 0.0992 |    1752 B |       15.64 |
| MaskObject_Medium        | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |     13,721.9 ns | 34.76 ns | 22.99 ns | 31.67 |    0.07 | 0.3815 |    6504 B |       58.07 |
| MaskObject_Large         | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |     24,499.3 ns | 93.91 ns | 62.11 ns | 56.54 |    0.16 | 1.0681 |   18312 B |      163.50 |
|                          |            |                |             |             |              |             |                 |          |          |       |         |        |           |             |
| Serialize_Small          | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 26,256,503.0 ns |       NA |  0.00 ns |  1.00 |    0.00 |      - |     112 B |        1.00 |
| Serialize_Medium         | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 26,961,517.0 ns |       NA |  0.00 ns |  1.03 |    0.00 |      - |     392 B |        3.50 |
| Serialize_Large          | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 30,353,983.0 ns |       NA |  0.00 ns |  1.16 |    0.00 |      - |    3784 B |       33.79 |
| SerializeRoundtrip_Small | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 33,925,083.0 ns |       NA |  0.00 ns |  1.29 |    0.00 |      - |     232 B |        2.07 |
| SerializeRoundtrip_Large | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 38,070,904.0 ns |       NA |  0.00 ns |  1.45 |    0.00 |      - |    7688 B |       68.64 |
| ParseAndModify_Small     | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 31,154,534.0 ns |       NA |  0.00 ns |  1.19 |    0.00 |      - |     840 B |        7.50 |
| MaskObject_Small         | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 61,922,618.0 ns |       NA |  0.00 ns |  2.36 |    0.00 |      - |    1752 B |       15.64 |
| MaskObject_Medium        | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 70,772,811.0 ns |       NA |  0.00 ns |  2.70 |    0.00 |      - |    6504 B |       58.07 |
| MaskObject_Large         | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 82,791,823.0 ns |       NA |  0.00 ns |  3.15 |    0.00 |      - |   18312 B |      163.50 |
