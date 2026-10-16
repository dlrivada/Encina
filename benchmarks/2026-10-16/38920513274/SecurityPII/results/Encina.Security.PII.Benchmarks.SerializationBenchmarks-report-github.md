```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                   | Job        | IterationCount | LaunchCount | Mean       | Error       | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |-----------:|------------:|----------:|------:|--------:|-------:|----------:|------------:|
| Serialize_Small          | Job-YFEFPZ | 10             | Default     |   440.0 ns |     2.73 ns |   1.63 ns |  1.00 |    0.00 | 0.0067 |     112 B |        1.00 |
| Serialize_Medium         | Job-YFEFPZ | 10             | Default     | 1,018.4 ns |     7.63 ns |   5.05 ns |  2.31 |    0.01 | 0.0229 |     392 B |        3.50 |
| Serialize_Large          | Job-YFEFPZ | 10             | Default     | 2,205.2 ns |    26.07 ns |  15.51 ns |  5.01 |    0.04 | 0.2251 |    3784 B |       33.79 |
| SerializeRoundtrip_Small | Job-YFEFPZ | 10             | Default     |   979.0 ns |     3.21 ns |   1.68 ns |  2.23 |    0.01 | 0.0134 |     232 B |        2.07 |
| SerializeRoundtrip_Large | Job-YFEFPZ | 10             | Default     | 5,053.5 ns |   158.59 ns | 104.90 ns | 11.49 |    0.23 | 0.4578 |    7688 B |       68.64 |
| ParseAndModify_Small     | Job-YFEFPZ | 10             | Default     | 1,891.3 ns |    10.25 ns |   6.78 ns |  4.30 |    0.02 | 0.0496 |     840 B |        7.50 |
| MaskObject_Small         | Job-YFEFPZ | 10             | Default     | 4,687.6 ns |    46.84 ns |  30.98 ns | 10.65 |    0.08 | 0.1144 |    2032 B |       18.14 |
| MaskObject_Medium        | Job-YFEFPZ | 10             | Default     |         NA |          NA |        NA |     ? |       ? |     NA |        NA |           ? |
| MaskObject_Large         | Job-YFEFPZ | 10             | Default     |         NA |          NA |        NA |     ? |       ? |     NA |        NA |           ? |
|                          |            |                |             |            |             |           |       |         |        |           |             |
| Serialize_Small          | ShortRun   | 3              | 1           |   443.4 ns |     9.76 ns |   0.53 ns |  1.00 |    0.00 | 0.0067 |     112 B |        1.00 |
| Serialize_Medium         | ShortRun   | 3              | 1           | 1,046.1 ns |   176.57 ns |   9.68 ns |  2.36 |    0.02 | 0.0229 |     392 B |        3.50 |
| Serialize_Large          | ShortRun   | 3              | 1           | 2,177.0 ns |   258.84 ns |  14.19 ns |  4.91 |    0.03 | 0.2251 |    3784 B |       33.79 |
| SerializeRoundtrip_Small | ShortRun   | 3              | 1           |   943.3 ns |    26.02 ns |   1.43 ns |  2.13 |    0.00 | 0.0134 |     232 B |        2.07 |
| SerializeRoundtrip_Large | ShortRun   | 3              | 1           | 5,066.5 ns | 1,043.62 ns |  57.20 ns | 11.43 |    0.11 | 0.4578 |    7688 B |       68.64 |
| ParseAndModify_Small     | ShortRun   | 3              | 1           | 1,816.3 ns |   189.31 ns |  10.38 ns |  4.10 |    0.02 | 0.0496 |     840 B |        7.50 |
| MaskObject_Small         | ShortRun   | 3              | 1           | 4,807.3 ns |   133.96 ns |   7.34 ns | 10.84 |    0.02 | 0.1144 |    2032 B |       18.14 |
| MaskObject_Medium        | ShortRun   | 3              | 1           |         NA |          NA |        NA |     ? |       ? |     NA |        NA |           ? |
| MaskObject_Large         | ShortRun   | 3              | 1           |         NA |          NA |        NA |     ? |       ? |     NA |        NA |           ? |

Benchmarks with issues:
  SerializationBenchmarks.MaskObject_Medium: Job-YFEFPZ(IterationCount=10, WarmupCount=3)
  SerializationBenchmarks.MaskObject_Large: Job-YFEFPZ(IterationCount=10, WarmupCount=3)
  SerializationBenchmarks.MaskObject_Medium: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
  SerializationBenchmarks.MaskObject_Large: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
