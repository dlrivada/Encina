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
| Serialize_Small          | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        442.9 ns |  1.37 ns |  0.72 ns |  1.00 |    0.00 | 0.0067 |     112 B |        1.00 |
| Serialize_Medium         | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1,040.7 ns |  3.10 ns |  2.05 ns |  2.35 |    0.01 | 0.0229 |     392 B |        3.50 |
| Serialize_Large          | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      2,147.6 ns | 41.94 ns | 27.74 ns |  4.85 |    0.06 | 0.2251 |    3784 B |       33.79 |
| SerializeRoundtrip_Small | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        946.8 ns |  7.83 ns |  4.66 ns |  2.14 |    0.01 | 0.0134 |     232 B |        2.07 |
| SerializeRoundtrip_Large | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4,785.6 ns |  7.52 ns |  4.98 ns | 10.80 |    0.02 | 0.4578 |    7688 B |       68.64 |
| ParseAndModify_Small     | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1,750.1 ns |  3.01 ns |  1.79 ns |  3.95 |    0.01 | 0.0496 |     840 B |        7.50 |
| MaskObject_Small         | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4,513.3 ns | 14.91 ns |  9.86 ns | 10.19 |    0.03 | 0.0992 |    1752 B |       15.64 |
| MaskObject_Medium        | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |     13,222.5 ns | 35.97 ns | 23.79 ns | 29.85 |    0.07 | 0.3815 |    6504 B |       58.07 |
| MaskObject_Large         | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |     23,656.7 ns | 49.00 ns | 29.16 ns | 53.41 |    0.10 | 1.0681 |   18312 B |      163.50 |
|                          |            |                |             |             |              |             |                 |          |          |       |         |        |           |             |
| Serialize_Small          | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 25,806,258.0 ns |       NA |  0.00 ns |  1.00 |    0.00 |      - |     112 B |        1.00 |
| Serialize_Medium         | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 25,924,158.0 ns |       NA |  0.00 ns |  1.00 |    0.00 |      - |     392 B |        3.50 |
| Serialize_Large          | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 29,291,469.0 ns |       NA |  0.00 ns |  1.14 |    0.00 |      - |    3784 B |       33.79 |
| SerializeRoundtrip_Small | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 32,150,658.0 ns |       NA |  0.00 ns |  1.25 |    0.00 |      - |     232 B |        2.07 |
| SerializeRoundtrip_Large | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 37,169,746.0 ns |       NA |  0.00 ns |  1.44 |    0.00 |      - |    7688 B |       68.64 |
| ParseAndModify_Small     | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 30,026,814.0 ns |       NA |  0.00 ns |  1.16 |    0.00 |      - |     840 B |        7.50 |
| MaskObject_Small         | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 59,221,742.0 ns |       NA |  0.00 ns |  2.29 |    0.00 |      - |    1752 B |       15.64 |
| MaskObject_Medium        | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 69,196,622.0 ns |       NA |  0.00 ns |  2.68 |    0.00 |      - |    6504 B |       58.07 |
| MaskObject_Large         | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 80,088,038.0 ns |       NA |  0.00 ns |  3.10 |    0.00 |      - |   18312 B |      163.50 |
