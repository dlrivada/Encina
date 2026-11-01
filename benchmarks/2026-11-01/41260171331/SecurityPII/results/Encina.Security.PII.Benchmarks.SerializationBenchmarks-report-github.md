```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.59GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3


```
| Method                   | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error    | StdDev   | Median     | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |------------ |-----------:|---------:|---------:|-----------:|------:|--------:|-------:|----------:|------------:|
| Serialize_Small          | Job-YFEFPZ | 10             | Default     | 3           |   435.5 ns |  1.02 ns |  0.60 ns |   435.2 ns |  1.00 |    0.00 | 0.0067 |     112 B |        1.00 |
| Serialize_Medium         | Job-YFEFPZ | 10             | Default     | 3           | 1,021.1 ns |  2.15 ns |  1.28 ns | 1,021.0 ns |  2.34 |    0.00 | 0.0229 |     392 B |        3.50 |
| Serialize_Large          | Job-YFEFPZ | 10             | Default     | 3           | 2,161.6 ns | 12.10 ns |  7.20 ns | 2,160.1 ns |  4.96 |    0.02 | 0.2251 |    3784 B |       33.79 |
| SerializeRoundtrip_Small | Job-YFEFPZ | 10             | Default     | 3           |   971.3 ns |  3.29 ns |  1.96 ns |   970.7 ns |  2.23 |    0.01 | 0.0134 |     232 B |        2.07 |
| SerializeRoundtrip_Large | Job-YFEFPZ | 10             | Default     | 3           | 4,884.2 ns | 14.77 ns |  8.79 ns | 4,884.6 ns | 11.21 |    0.02 | 0.4578 |    7688 B |       68.64 |
| ParseAndModify_Small     | Job-YFEFPZ | 10             | Default     | 3           | 1,791.5 ns |  4.83 ns |  2.53 ns | 1,791.2 ns |  4.11 |    0.01 | 0.0496 |     840 B |        7.50 |
| MaskObject_Small         | Job-YFEFPZ | 10             | Default     | 3           | 4,681.6 ns |  8.85 ns |  4.63 ns | 4,680.6 ns | 10.75 |    0.02 | 0.1144 |    2032 B |       18.14 |
| MaskObject_Medium        | Job-YFEFPZ | 10             | Default     | 3           |         NA |       NA |       NA |         NA |     ? |       ? |     NA |        NA |           ? |
| MaskObject_Large         | Job-YFEFPZ | 10             | Default     | 3           |         NA |       NA |       NA |         NA |     ? |       ? |     NA |        NA |           ? |
|                          |            |                |             |             |            |          |          |            |       |         |        |           |             |
| Serialize_Small          | MediumRun  | 15             | 2           | 10          |   443.8 ns |  3.15 ns |  4.52 ns |   440.9 ns |  1.00 |    0.01 | 0.0067 |     112 B |        1.00 |
| Serialize_Medium         | MediumRun  | 15             | 2           | 10          | 1,002.3 ns |  5.43 ns |  7.78 ns | 1,003.7 ns |  2.26 |    0.03 | 0.0229 |     392 B |        3.50 |
| Serialize_Large          | MediumRun  | 15             | 2           | 10          | 2,131.3 ns |  3.32 ns |  4.87 ns | 2,132.2 ns |  4.80 |    0.05 | 0.2251 |    3784 B |       33.79 |
| SerializeRoundtrip_Small | MediumRun  | 15             | 2           | 10          |   941.0 ns |  3.08 ns |  4.41 ns |   941.6 ns |  2.12 |    0.02 | 0.0134 |     232 B |        2.07 |
| SerializeRoundtrip_Large | MediumRun  | 15             | 2           | 10          | 4,920.7 ns | 67.81 ns | 97.25 ns | 4,885.4 ns | 11.09 |    0.24 | 0.4578 |    7688 B |       68.64 |
| ParseAndModify_Small     | MediumRun  | 15             | 2           | 10          | 1,797.6 ns |  6.77 ns |  9.92 ns | 1,795.6 ns |  4.05 |    0.05 | 0.0496 |     840 B |        7.50 |
| MaskObject_Small         | MediumRun  | 15             | 2           | 10          | 4,891.3 ns | 47.96 ns | 67.23 ns | 4,859.6 ns | 11.02 |    0.19 | 0.1144 |    2032 B |       18.14 |
| MaskObject_Medium        | MediumRun  | 15             | 2           | 10          |         NA |       NA |       NA |         NA |     ? |       ? |     NA |        NA |           ? |
| MaskObject_Large         | MediumRun  | 15             | 2           | 10          |         NA |       NA |       NA |         NA |     ? |       ? |     NA |        NA |           ? |

Benchmarks with issues:
  SerializationBenchmarks.MaskObject_Medium: Job-YFEFPZ(IterationCount=10, WarmupCount=3)
  SerializationBenchmarks.MaskObject_Large: Job-YFEFPZ(IterationCount=10, WarmupCount=3)
  SerializationBenchmarks.MaskObject_Medium: MediumRun(IterationCount=15, LaunchCount=2, WarmupCount=10)
  SerializationBenchmarks.MaskObject_Large: MediumRun(IterationCount=15, LaunchCount=2, WarmupCount=10)
