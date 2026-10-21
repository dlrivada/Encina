```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.61GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean         | Error       | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-------------:|------------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |     4.004 ns |   0.8328 ns | 0.0456 ns |   0.50 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |     8.034 ns |   0.3056 ns | 0.0168 ns |   1.00 |    0.00 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   742.449 ns | 119.8161 ns | 6.5675 ns |  92.41 |    0.73 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,154.733 ns |  53.7024 ns | 2.9436 ns | 143.72 |    0.41 | 0.0286 |     504 B |       12.60 |
