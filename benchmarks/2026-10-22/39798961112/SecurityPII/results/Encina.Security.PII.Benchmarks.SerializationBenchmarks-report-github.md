```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4


```
| Method                   | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error    | StdDev   | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |------------ |-----------:|---------:|---------:|------:|--------:|-------:|-------:|----------:|------------:|
| Serialize_Small          | Job-YFEFPZ | 10             | Default     | 3           |   221.2 ns |  7.70 ns |  5.09 ns |  1.00 |    0.03 | 0.0067 |      - |     112 B |        1.00 |
| Serialize_Medium         | Job-YFEFPZ | 10             | Default     | 3           |   511.3 ns |  7.16 ns |  4.26 ns |  2.31 |    0.05 | 0.0229 |      - |     392 B |        3.50 |
| Serialize_Large          | Job-YFEFPZ | 10             | Default     | 3           | 1,121.4 ns | 12.40 ns |  7.38 ns |  5.07 |    0.11 | 0.2251 |      - |    3784 B |       33.79 |
| SerializeRoundtrip_Small | Job-YFEFPZ | 10             | Default     | 3           |   535.2 ns |  6.78 ns |  4.03 ns |  2.42 |    0.06 | 0.0134 |      - |     232 B |        2.07 |
| SerializeRoundtrip_Large | Job-YFEFPZ | 10             | Default     | 3           | 2,636.8 ns | 33.43 ns | 17.48 ns | 11.92 |    0.27 | 0.4578 | 0.0038 |    7688 B |       68.64 |
| ParseAndModify_Small     | Job-YFEFPZ | 10             | Default     | 3           |   985.7 ns | 13.25 ns |  7.88 ns |  4.46 |    0.10 | 0.0496 |      - |     840 B |        7.50 |
| MaskObject_Small         | Job-YFEFPZ | 10             | Default     | 3           | 2,427.3 ns | 37.12 ns | 24.55 ns | 10.98 |    0.26 | 0.1183 |      - |    2032 B |       18.14 |
| MaskObject_Medium        | Job-YFEFPZ | 10             | Default     | 3           |         NA |       NA |       NA |     ? |       ? |     NA |     NA |        NA |           ? |
| MaskObject_Large         | Job-YFEFPZ | 10             | Default     | 3           |         NA |       NA |       NA |     ? |       ? |     NA |     NA |        NA |           ? |
|                          |            |                |             |             |            |          |          |       |         |        |        |           |             |
| Serialize_Small          | MediumRun  | 15             | 2           | 10          |   222.3 ns |  3.08 ns |  4.52 ns |  1.00 |    0.03 | 0.0067 |      - |     112 B |        1.00 |
| Serialize_Medium         | MediumRun  | 15             | 2           | 10          |   559.3 ns | 19.35 ns | 26.49 ns |  2.52 |    0.13 | 0.0229 |      - |     392 B |        3.50 |
| Serialize_Large          | MediumRun  | 15             | 2           | 10          | 1,075.4 ns | 15.77 ns | 22.62 ns |  4.84 |    0.14 | 0.2251 |      - |    3784 B |       33.79 |
| SerializeRoundtrip_Small | MediumRun  | 15             | 2           | 10          |   537.6 ns |  5.50 ns |  7.71 ns |  2.42 |    0.06 | 0.0134 |      - |     232 B |        2.07 |
| SerializeRoundtrip_Large | MediumRun  | 15             | 2           | 10          | 2,718.4 ns | 39.73 ns | 58.24 ns | 12.23 |    0.35 | 0.4578 | 0.0038 |    7688 B |       68.64 |
| ParseAndModify_Small     | MediumRun  | 15             | 2           | 10          |   984.9 ns |  7.25 ns | 10.63 ns |  4.43 |    0.10 | 0.0496 |      - |     840 B |        7.50 |
| MaskObject_Small         | MediumRun  | 15             | 2           | 10          | 2,373.4 ns | 22.50 ns | 30.80 ns | 10.68 |    0.25 | 0.1183 |      - |    2032 B |       18.14 |
| MaskObject_Medium        | MediumRun  | 15             | 2           | 10          |         NA |       NA |       NA |     ? |       ? |     NA |     NA |        NA |           ? |
| MaskObject_Large         | MediumRun  | 15             | 2           | 10          |         NA |       NA |       NA |     ? |       ? |     NA |     NA |        NA |           ? |

Benchmarks with issues:
  SerializationBenchmarks.MaskObject_Medium: Job-YFEFPZ(IterationCount=10, WarmupCount=3)
  SerializationBenchmarks.MaskObject_Large: Job-YFEFPZ(IterationCount=10, WarmupCount=3)
  SerializationBenchmarks.MaskObject_Medium: MediumRun(IterationCount=15, LaunchCount=2, WarmupCount=10)
  SerializationBenchmarks.MaskObject_Large: MediumRun(IterationCount=15, LaunchCount=2, WarmupCount=10)
