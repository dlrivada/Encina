```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                   | Job        | IterationCount | LaunchCount | Mean       | Error       | StdDev   | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |-----------:|------------:|---------:|------:|--------:|-------:|-------:|----------:|------------:|
| Serialize_Small          | Job-YFEFPZ | 10             | Default     |   329.0 ns |     1.05 ns |  0.63 ns |  1.00 |    0.00 | 0.0067 |      - |     112 B |        1.00 |
| Serialize_Medium         | Job-YFEFPZ | 10             | Default     |   768.7 ns |     1.80 ns |  1.19 ns |  2.34 |    0.01 | 0.0229 |      - |     392 B |        3.50 |
| Serialize_Large          | Job-YFEFPZ | 10             | Default     | 1,902.1 ns |    46.76 ns | 30.93 ns |  5.78 |    0.09 | 0.2251 |      - |    3784 B |       33.79 |
| SerializeRoundtrip_Small | Job-YFEFPZ | 10             | Default     |   686.7 ns |     1.18 ns |  0.78 ns |  2.09 |    0.00 | 0.0134 |      - |     232 B |        2.07 |
| SerializeRoundtrip_Large | Job-YFEFPZ | 10             | Default     | 3,967.4 ns |   101.79 ns | 67.33 ns | 12.06 |    0.20 | 0.4578 |      - |    7688 B |       68.64 |
| ParseAndModify_Small     | Job-YFEFPZ | 10             | Default     | 1,279.4 ns |     4.53 ns |  2.37 ns |  3.89 |    0.01 | 0.0496 |      - |     840 B |        7.50 |
| MaskObject_Small         | Job-YFEFPZ | 10             | Default     | 3,287.7 ns |    12.61 ns |  7.50 ns |  9.99 |    0.03 | 0.1183 |      - |    2032 B |       18.14 |
| MaskObject_Medium        | Job-YFEFPZ | 10             | Default     |         NA |          NA |       NA |     ? |       ? |     NA |     NA |        NA |           ? |
| MaskObject_Large         | Job-YFEFPZ | 10             | Default     |         NA |          NA |       NA |     ? |       ? |     NA |     NA |        NA |           ? |
|                          |            |                |             |            |             |          |       |         |        |        |           |             |
| Serialize_Small          | ShortRun   | 3              | 1           |   325.1 ns |    15.56 ns |  0.85 ns |  1.00 |    0.00 | 0.0067 |      - |     112 B |        1.00 |
| Serialize_Medium         | ShortRun   | 3              | 1           |   765.3 ns |    30.95 ns |  1.70 ns |  2.35 |    0.01 | 0.0229 |      - |     392 B |        3.50 |
| Serialize_Large          | ShortRun   | 3              | 1           | 1,748.5 ns | 1,051.81 ns | 57.65 ns |  5.38 |    0.15 | 0.2251 |      - |    3784 B |       33.79 |
| SerializeRoundtrip_Small | ShortRun   | 3              | 1           |   712.6 ns |    17.87 ns |  0.98 ns |  2.19 |    0.01 | 0.0134 |      - |     232 B |        2.07 |
| SerializeRoundtrip_Large | ShortRun   | 3              | 1           | 3,891.9 ns |   562.19 ns | 30.82 ns | 11.97 |    0.09 | 0.4578 | 0.0038 |    7688 B |       68.64 |
| ParseAndModify_Small     | ShortRun   | 3              | 1           | 1,290.0 ns |   133.95 ns |  7.34 ns |  3.97 |    0.02 | 0.0496 |      - |     840 B |        7.50 |
| MaskObject_Small         | ShortRun   | 3              | 1           | 3,331.8 ns |   494.29 ns | 27.09 ns | 10.25 |    0.08 | 0.1183 |      - |    2032 B |       18.14 |
| MaskObject_Medium        | ShortRun   | 3              | 1           |         NA |          NA |       NA |     ? |       ? |     NA |     NA |        NA |           ? |
| MaskObject_Large         | ShortRun   | 3              | 1           |         NA |          NA |       NA |     ? |       ? |     NA |     NA |        NA |           ? |

Benchmarks with issues:
  SerializationBenchmarks.MaskObject_Medium: Job-YFEFPZ(IterationCount=10, WarmupCount=3)
  SerializationBenchmarks.MaskObject_Large: Job-YFEFPZ(IterationCount=10, WarmupCount=3)
  SerializationBenchmarks.MaskObject_Medium: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
  SerializationBenchmarks.MaskObject_Large: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
