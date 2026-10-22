```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.15GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                   | Job        | IterationCount | LaunchCount | Mean       | Error     | StdDev   | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |-----------:|----------:|---------:|------:|--------:|-------:|-------:|----------:|------------:|
| Serialize_Small          | Job-YFEFPZ | 10             | Default     |   237.6 ns |   5.83 ns |  3.85 ns |  1.00 |    0.02 | 0.0067 |      - |     112 B |        1.00 |
| Serialize_Medium         | Job-YFEFPZ | 10             | Default     |   529.5 ns |   6.92 ns |  4.57 ns |  2.23 |    0.04 | 0.0229 |      - |     392 B |        3.50 |
| Serialize_Large          | Job-YFEFPZ | 10             | Default     | 1,086.9 ns |  13.08 ns |  7.78 ns |  4.58 |    0.08 | 0.2251 |      - |    3784 B |       33.79 |
| SerializeRoundtrip_Small | Job-YFEFPZ | 10             | Default     |   559.3 ns |  20.77 ns | 13.74 ns |  2.35 |    0.07 | 0.0134 |      - |     232 B |        2.07 |
| SerializeRoundtrip_Large | Job-YFEFPZ | 10             | Default     | 2,574.8 ns |  54.30 ns | 32.31 ns | 10.84 |    0.21 | 0.4578 | 0.0038 |    7688 B |       68.64 |
| ParseAndModify_Small     | Job-YFEFPZ | 10             | Default     | 1,002.9 ns |  32.93 ns | 19.60 ns |  4.22 |    0.10 | 0.0496 |      - |     840 B |        7.50 |
| MaskObject_Small         | Job-YFEFPZ | 10             | Default     | 2,451.8 ns |  57.05 ns | 37.74 ns | 10.32 |    0.22 | 0.1183 |      - |    2032 B |       18.14 |
| MaskObject_Medium        | Job-YFEFPZ | 10             | Default     |         NA |        NA |       NA |     ? |       ? |     NA |     NA |        NA |           ? |
| MaskObject_Large         | Job-YFEFPZ | 10             | Default     |         NA |        NA |       NA |     ? |       ? |     NA |     NA |        NA |           ? |
|                          |            |                |             |            |           |          |       |         |        |        |           |             |
| Serialize_Small          | ShortRun   | 3              | 1           |   229.5 ns |  63.17 ns |  3.46 ns |  1.00 |    0.02 | 0.0067 |      - |     112 B |        1.00 |
| Serialize_Medium         | ShortRun   | 3              | 1           |   521.7 ns |  45.66 ns |  2.50 ns |  2.27 |    0.03 | 0.0229 |      - |     392 B |        3.50 |
| Serialize_Large          | ShortRun   | 3              | 1           | 1,191.1 ns |  66.59 ns |  3.65 ns |  5.19 |    0.07 | 0.2251 |      - |    3784 B |       33.79 |
| SerializeRoundtrip_Small | ShortRun   | 3              | 1           |   556.1 ns | 103.50 ns |  5.67 ns |  2.42 |    0.04 | 0.0134 |      - |     232 B |        2.07 |
| SerializeRoundtrip_Large | ShortRun   | 3              | 1           | 2,614.2 ns | 705.14 ns | 38.65 ns | 11.39 |    0.21 | 0.4578 | 0.0038 |    7688 B |       68.64 |
| ParseAndModify_Small     | ShortRun   | 3              | 1           |   987.6 ns | 787.87 ns | 43.19 ns |  4.30 |    0.17 | 0.0496 |      - |     840 B |        7.50 |
| MaskObject_Small         | ShortRun   | 3              | 1           | 2,408.0 ns | 989.64 ns | 54.25 ns | 10.49 |    0.25 | 0.1183 |      - |    2032 B |       18.14 |
| MaskObject_Medium        | ShortRun   | 3              | 1           |         NA |        NA |       NA |     ? |       ? |     NA |     NA |        NA |           ? |
| MaskObject_Large         | ShortRun   | 3              | 1           |         NA |        NA |       NA |     ? |       ? |     NA |     NA |        NA |           ? |

Benchmarks with issues:
  SerializationBenchmarks.MaskObject_Medium: Job-YFEFPZ(IterationCount=10, WarmupCount=3)
  SerializationBenchmarks.MaskObject_Large: Job-YFEFPZ(IterationCount=10, WarmupCount=3)
  SerializationBenchmarks.MaskObject_Medium: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
  SerializationBenchmarks.MaskObject_Large: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
