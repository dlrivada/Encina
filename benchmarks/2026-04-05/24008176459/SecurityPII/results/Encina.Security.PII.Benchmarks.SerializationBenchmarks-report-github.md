```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                   | Job        | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount | Mean            | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |------------ |------------- |------------ |----------------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Serialize_Small          | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        443.9 ns |   0.65 ns |   0.39 ns |  1.00 |    0.00 | 0.0067 |     112 B |        1.00 |
| Serialize_Medium         | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1,004.7 ns |   1.75 ns |   1.04 ns |  2.26 |    0.00 | 0.0229 |     392 B |        3.50 |
| Serialize_Large          | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      2,216.4 ns |  13.97 ns |   8.31 ns |  4.99 |    0.02 | 0.2251 |    3784 B |       33.79 |
| SerializeRoundtrip_Small | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        920.0 ns |   1.30 ns |   0.68 ns |  2.07 |    0.00 | 0.0134 |     232 B |        2.07 |
| SerializeRoundtrip_Large | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4,975.1 ns |  61.64 ns |  40.77 ns | 11.21 |    0.09 | 0.4578 |    7688 B |       68.64 |
| ParseAndModify_Small     | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1,782.8 ns |   7.23 ns |   4.78 ns |  4.02 |    0.01 | 0.0496 |     840 B |        7.50 |
| MaskObject_Small         | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4,573.6 ns |  13.17 ns |   8.71 ns | 10.30 |    0.02 | 0.0992 |    1752 B |       15.64 |
| MaskObject_Medium        | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |     13,658.6 ns |  61.12 ns |  36.37 ns | 30.77 |    0.08 | 0.3815 |    6504 B |       58.07 |
| MaskObject_Large         | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |     23,496.5 ns | 177.30 ns | 117.27 ns | 52.93 |    0.26 | 1.0681 |   18312 B |      163.50 |
|                          |            |                |             |             |              |             |                 |           |           |       |         |        |           |             |
| Serialize_Small          | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 26,277,200.0 ns |        NA |   0.00 ns |  1.00 |    0.00 |      - |     112 B |        1.00 |
| Serialize_Medium         | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 26,987,529.0 ns |        NA |   0.00 ns |  1.03 |    0.00 |      - |     392 B |        3.50 |
| Serialize_Large          | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 29,399,183.0 ns |        NA |   0.00 ns |  1.12 |    0.00 |      - |    3784 B |       33.79 |
| SerializeRoundtrip_Small | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 32,716,922.0 ns |        NA |   0.00 ns |  1.25 |    0.00 |      - |     232 B |        2.07 |
| SerializeRoundtrip_Large | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 37,703,695.0 ns |        NA |   0.00 ns |  1.43 |    0.00 |      - |    7688 B |       68.64 |
| ParseAndModify_Small     | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 29,853,383.0 ns |        NA |   0.00 ns |  1.14 |    0.00 |      - |     840 B |        7.50 |
| MaskObject_Small         | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 60,182,966.0 ns |        NA |   0.00 ns |  2.29 |    0.00 |      - |    1752 B |       15.64 |
| MaskObject_Medium        | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 69,197,087.0 ns |        NA |   0.00 ns |  2.63 |    0.00 |      - |    6504 B |       58.07 |
| MaskObject_Large         | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 79,837,121.0 ns |        NA |   0.00 ns |  3.04 |    0.00 |      - |   18312 B |      163.50 |
