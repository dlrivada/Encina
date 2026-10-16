```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                   | Job        | IterationCount | LaunchCount | Mean       | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |-----------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| Serialize_Small          | Job-YFEFPZ | 10             | Default     |   408.1 ns |   3.84 ns |  2.54 ns |  1.00 |    0.01 | 0.0043 |     112 B |        1.00 |
| Serialize_Medium         | Job-YFEFPZ | 10             | Default     |   981.9 ns |  12.14 ns |  8.03 ns |  2.41 |    0.02 | 0.0153 |     392 B |        3.50 |
| Serialize_Large          | Job-YFEFPZ | 10             | Default     | 2,195.1 ns |  39.97 ns | 26.44 ns |  5.38 |    0.07 | 0.1488 |    3784 B |       33.79 |
| SerializeRoundtrip_Small | Job-YFEFPZ | 10             | Default     |   946.9 ns |   3.52 ns |  2.09 ns |  2.32 |    0.01 | 0.0086 |     232 B |        2.07 |
| SerializeRoundtrip_Large | Job-YFEFPZ | 10             | Default     | 5,414.1 ns |  33.83 ns | 20.13 ns | 13.27 |    0.09 | 0.3052 |    7688 B |       68.64 |
| ParseAndModify_Small     | Job-YFEFPZ | 10             | Default     | 1,699.2 ns |   2.80 ns |  1.67 ns |  4.16 |    0.03 | 0.0324 |     840 B |        7.50 |
| MaskObject_Small         | Job-YFEFPZ | 10             | Default     | 5,131.3 ns |   6.71 ns |  4.00 ns | 12.57 |    0.08 | 0.0763 |    2032 B |       18.14 |
| MaskObject_Medium        | Job-YFEFPZ | 10             | Default     |         NA |        NA |       NA |     ? |       ? |     NA |        NA |           ? |
| MaskObject_Large         | Job-YFEFPZ | 10             | Default     |         NA |        NA |       NA |     ? |       ? |     NA |        NA |           ? |
|                          |            |                |             |            |           |          |       |         |        |           |             |
| Serialize_Small          | ShortRun   | 3              | 1           |   406.5 ns | 177.85 ns |  9.75 ns |  1.00 |    0.03 | 0.0043 |     112 B |        1.00 |
| Serialize_Medium         | ShortRun   | 3              | 1           |   981.4 ns |  13.88 ns |  0.76 ns |  2.42 |    0.05 | 0.0153 |     392 B |        3.50 |
| Serialize_Large          | ShortRun   | 3              | 1           | 2,239.8 ns | 219.35 ns | 12.02 ns |  5.51 |    0.12 | 0.1488 |    3784 B |       33.79 |
| SerializeRoundtrip_Small | ShortRun   | 3              | 1           |   936.7 ns |   6.50 ns |  0.36 ns |  2.30 |    0.05 | 0.0086 |     232 B |        2.07 |
| SerializeRoundtrip_Large | ShortRun   | 3              | 1           | 5,639.5 ns | 948.45 ns | 51.99 ns | 13.88 |    0.31 | 0.3052 |    7688 B |       68.64 |
| ParseAndModify_Small     | ShortRun   | 3              | 1           | 1,734.0 ns |  51.08 ns |  2.80 ns |  4.27 |    0.09 | 0.0324 |     840 B |        7.50 |
| MaskObject_Small         | ShortRun   | 3              | 1           | 5,272.8 ns | 272.96 ns | 14.96 ns | 12.97 |    0.27 | 0.0763 |    2032 B |       18.14 |
| MaskObject_Medium        | ShortRun   | 3              | 1           |         NA |        NA |       NA |     ? |       ? |     NA |        NA |           ? |
| MaskObject_Large         | ShortRun   | 3              | 1           |         NA |        NA |       NA |     ? |       ? |     NA |        NA |           ? |

Benchmarks with issues:
  SerializationBenchmarks.MaskObject_Medium: Job-YFEFPZ(IterationCount=10, WarmupCount=3)
  SerializationBenchmarks.MaskObject_Large: Job-YFEFPZ(IterationCount=10, WarmupCount=3)
  SerializationBenchmarks.MaskObject_Medium: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
  SerializationBenchmarks.MaskObject_Large: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
