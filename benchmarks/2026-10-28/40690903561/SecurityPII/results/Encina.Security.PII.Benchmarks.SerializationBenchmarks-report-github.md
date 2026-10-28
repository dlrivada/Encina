```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                   | Job        | IterationCount | LaunchCount | Mean       | Error     | StdDev   | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |-----------:|----------:|---------:|------:|--------:|-------:|-------:|----------:|------------:|
| Serialize_Small          | Job-YFEFPZ | 10             | Default     |   315.9 ns |   1.64 ns |  1.09 ns |  1.00 |    0.00 | 0.0067 |      - |     112 B |        1.00 |
| Serialize_Medium         | Job-YFEFPZ | 10             | Default     |   775.7 ns |   2.11 ns |  1.39 ns |  2.46 |    0.01 | 0.0229 |      - |     392 B |        3.50 |
| Serialize_Large          | Job-YFEFPZ | 10             | Default     | 1,647.3 ns |   8.67 ns |  5.16 ns |  5.21 |    0.02 | 0.2251 |      - |    3784 B |       33.79 |
| SerializeRoundtrip_Small | Job-YFEFPZ | 10             | Default     |   685.3 ns |   1.39 ns |  0.92 ns |  2.17 |    0.01 | 0.0134 |      - |     232 B |        2.07 |
| SerializeRoundtrip_Large | Job-YFEFPZ | 10             | Default     | 3,768.6 ns |  43.10 ns | 25.65 ns | 11.93 |    0.09 | 0.4578 | 0.0038 |    7688 B |       68.64 |
| ParseAndModify_Small     | Job-YFEFPZ | 10             | Default     | 1,272.5 ns |  11.90 ns |  7.87 ns |  4.03 |    0.03 | 0.0496 |      - |     840 B |        7.50 |
| MaskObject_Small         | Job-YFEFPZ | 10             | Default     | 3,313.3 ns |  44.10 ns | 29.17 ns | 10.49 |    0.09 | 0.1183 |      - |    2032 B |       18.14 |
| MaskObject_Medium        | Job-YFEFPZ | 10             | Default     |         NA |        NA |       NA |     ? |       ? |     NA |     NA |        NA |           ? |
| MaskObject_Large         | Job-YFEFPZ | 10             | Default     |         NA |        NA |       NA |     ? |       ? |     NA |     NA |        NA |           ? |
|                          |            |                |             |            |           |          |       |         |        |        |           |             |
| Serialize_Small          | ShortRun   | 3              | 1           |   340.0 ns |  45.24 ns |  2.48 ns |  1.00 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| Serialize_Medium         | ShortRun   | 3              | 1           |   780.4 ns | 263.98 ns | 14.47 ns |  2.30 |    0.04 | 0.0229 |      - |     392 B |        3.50 |
| Serialize_Large          | ShortRun   | 3              | 1           | 1,700.6 ns | 686.00 ns | 37.60 ns |  5.00 |    0.10 | 0.2251 |      - |    3784 B |       33.79 |
| SerializeRoundtrip_Small | ShortRun   | 3              | 1           |   707.7 ns |  16.51 ns |  0.90 ns |  2.08 |    0.01 | 0.0134 |      - |     232 B |        2.07 |
| SerializeRoundtrip_Large | ShortRun   | 3              | 1           | 3,887.7 ns | 874.84 ns | 47.95 ns | 11.43 |    0.14 | 0.4578 |      - |    7688 B |       68.64 |
| ParseAndModify_Small     | ShortRun   | 3              | 1           | 1,288.4 ns | 161.03 ns |  8.83 ns |  3.79 |    0.03 | 0.0496 |      - |     840 B |        7.50 |
| MaskObject_Small         | ShortRun   | 3              | 1           | 3,334.1 ns |  49.94 ns |  2.74 ns |  9.81 |    0.06 | 0.1183 |      - |    2032 B |       18.14 |
| MaskObject_Medium        | ShortRun   | 3              | 1           |         NA |        NA |       NA |     ? |       ? |     NA |     NA |        NA |           ? |
| MaskObject_Large         | ShortRun   | 3              | 1           |         NA |        NA |       NA |     ? |       ? |     NA |     NA |        NA |           ? |

Benchmarks with issues:
  SerializationBenchmarks.MaskObject_Medium: Job-YFEFPZ(IterationCount=10, WarmupCount=3)
  SerializationBenchmarks.MaskObject_Large: Job-YFEFPZ(IterationCount=10, WarmupCount=3)
  SerializationBenchmarks.MaskObject_Medium: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
  SerializationBenchmarks.MaskObject_Large: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
