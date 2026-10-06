```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.94GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean         | Error       | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-------------:|------------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |     3.975 ns |   0.1376 ns | 0.0075 ns |   0.49 |    0.00 |      - |         - |        0.00 |
| CreateGtidPosition   |     8.085 ns |   0.7242 ns | 0.0397 ns |   1.00 |    0.01 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   774.051 ns |  51.8251 ns | 2.8407 ns |  95.74 |    0.51 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,222.498 ns | 109.3303 ns | 5.9928 ns | 151.20 |    0.91 | 0.0286 |     504 B |       12.60 |
