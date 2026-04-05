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
| Serialize_Small          | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        443.7 ns |  0.78 ns |  0.51 ns |  1.00 |    0.00 | 0.0067 |     112 B |        1.00 |
| Serialize_Medium         | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1,037.8 ns |  5.09 ns |  3.37 ns |  2.34 |    0.01 | 0.0229 |     392 B |        3.50 |
| Serialize_Large          | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      2,120.6 ns |  5.81 ns |  3.04 ns |  4.78 |    0.01 | 0.2251 |    3784 B |       33.79 |
| SerializeRoundtrip_Small | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        921.5 ns |  2.21 ns |  1.46 ns |  2.08 |    0.00 | 0.0134 |     232 B |        2.07 |
| SerializeRoundtrip_Large | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4,933.9 ns | 20.08 ns | 13.28 ns | 11.12 |    0.03 | 0.4578 |    7688 B |       68.64 |
| ParseAndModify_Small     | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1,798.4 ns |  6.79 ns |  4.49 ns |  4.05 |    0.01 | 0.0496 |     840 B |        7.50 |
| MaskObject_Small         | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4,484.4 ns | 10.45 ns |  6.91 ns | 10.11 |    0.02 | 0.0992 |    1752 B |       15.64 |
| MaskObject_Medium        | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |     13,304.7 ns | 57.16 ns | 37.81 ns | 29.99 |    0.09 | 0.3815 |    6504 B |       58.07 |
| MaskObject_Large         | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |     23,540.3 ns | 50.84 ns | 33.63 ns | 53.06 |    0.09 | 1.0681 |   18312 B |      163.50 |
|                          |            |                |             |             |              |             |                 |          |          |       |         |        |           |             |
| Serialize_Small          | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 25,398,228.0 ns |       NA |  0.00 ns |  1.00 |    0.00 |      - |     112 B |        1.00 |
| Serialize_Medium         | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 25,836,913.0 ns |       NA |  0.00 ns |  1.02 |    0.00 |      - |     392 B |        3.50 |
| Serialize_Large          | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 28,796,748.0 ns |       NA |  0.00 ns |  1.13 |    0.00 |      - |    3784 B |       33.79 |
| SerializeRoundtrip_Small | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 32,153,760.0 ns |       NA |  0.00 ns |  1.27 |    0.00 |      - |     232 B |        2.07 |
| SerializeRoundtrip_Large | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 37,018,838.0 ns |       NA |  0.00 ns |  1.46 |    0.00 |      - |    7688 B |       68.64 |
| ParseAndModify_Small     | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 29,517,996.0 ns |       NA |  0.00 ns |  1.16 |    0.00 |      - |     840 B |        7.50 |
| MaskObject_Small         | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 59,065,267.0 ns |       NA |  0.00 ns |  2.33 |    0.00 |      - |    1752 B |       15.64 |
| MaskObject_Medium        | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 69,650,183.0 ns |       NA |  0.00 ns |  2.74 |    0.00 |      - |    6504 B |       58.07 |
| MaskObject_Large         | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 76,692,277.0 ns |       NA |  0.00 ns |  3.02 |    0.00 |      - |   18312 B |      163.50 |
