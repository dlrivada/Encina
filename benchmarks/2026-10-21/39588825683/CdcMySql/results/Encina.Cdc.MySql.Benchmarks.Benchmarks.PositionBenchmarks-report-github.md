```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean       | Error      | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-----------:|-----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |   2.671 ns |  0.4947 ns | 0.0271 ns |   0.36 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |   7.489 ns |  4.5657 ns | 0.2503 ns |   1.00 |    0.04 | 0.0024 |      40 B |        1.00 |
| FromBytes            | 546.549 ns | 74.3308 ns | 4.0743 ns |  73.03 |    2.19 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 837.535 ns | 16.1792 ns | 0.8868 ns | 111.92 |    3.28 | 0.0296 |     504 B |       12.60 |
