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
| Serialize_Small          | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        479.1 ns |   3.11 ns |   2.06 ns |  1.00 |    0.01 | 0.0067 |     112 B |        1.00 |
| Serialize_Medium         | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1,162.3 ns |   4.76 ns |   2.83 ns |  2.43 |    0.01 | 0.0229 |     392 B |        3.50 |
| Serialize_Large          | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      2,519.9 ns |  46.85 ns |  30.99 ns |  5.26 |    0.07 | 0.2251 |    3784 B |       33.79 |
| SerializeRoundtrip_Small | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        941.6 ns |   4.12 ns |   2.15 ns |  1.97 |    0.01 | 0.0134 |     232 B |        2.07 |
| SerializeRoundtrip_Large | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      5,271.3 ns |  19.77 ns |  10.34 ns | 11.00 |    0.05 | 0.4578 |    7688 B |       68.64 |
| ParseAndModify_Small     | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1,804.5 ns |   4.01 ns |   2.65 ns |  3.77 |    0.02 | 0.0496 |     840 B |        7.50 |
| MaskObject_Small         | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4,573.5 ns |  13.45 ns |   8.00 ns |  9.55 |    0.04 | 0.0992 |    1752 B |       15.64 |
| MaskObject_Medium        | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |     13,853.9 ns | 253.37 ns | 167.59 ns | 28.91 |    0.35 | 0.3815 |    6504 B |       58.07 |
| MaskObject_Large         | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |     24,230.3 ns |  95.37 ns |  63.08 ns | 50.57 |    0.24 | 1.0681 |   18312 B |      163.50 |
|                          |            |                |             |             |              |             |                 |           |           |       |         |        |           |             |
| Serialize_Small          | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 26,450,134.0 ns |        NA |   0.00 ns |  1.00 |    0.00 |      - |     112 B |        1.00 |
| Serialize_Medium         | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 26,914,132.0 ns |        NA |   0.00 ns |  1.02 |    0.00 |      - |     392 B |        3.50 |
| Serialize_Large          | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 30,005,895.0 ns |        NA |   0.00 ns |  1.13 |    0.00 |      - |    3784 B |       33.79 |
| SerializeRoundtrip_Small | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 32,713,219.0 ns |        NA |   0.00 ns |  1.24 |    0.00 |      - |     232 B |        2.07 |
| SerializeRoundtrip_Large | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 38,594,470.0 ns |        NA |   0.00 ns |  1.46 |    0.00 |      - |    7688 B |       68.64 |
| ParseAndModify_Small     | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 31,477,598.0 ns |        NA |   0.00 ns |  1.19 |    0.00 |      - |     840 B |        7.50 |
| MaskObject_Small         | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 61,637,311.0 ns |        NA |   0.00 ns |  2.33 |    0.00 |      - |    1752 B |       15.64 |
| MaskObject_Medium        | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 70,991,135.0 ns |        NA |   0.00 ns |  2.68 |    0.00 |      - |    6504 B |       58.07 |
| MaskObject_Large         | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 80,903,038.0 ns |        NA |   0.00 ns |  3.06 |    0.00 |      - |   18312 B |      163.50 |
