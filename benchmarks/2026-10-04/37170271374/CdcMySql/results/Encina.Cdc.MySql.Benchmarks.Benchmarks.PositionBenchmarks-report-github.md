```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method               | Mean         | Error     | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-------------:|----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |     3.340 ns | 0.0055 ns | 0.0078 ns |   0.45 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |     7.388 ns | 0.1325 ns | 0.1984 ns |   1.00 |    0.04 | 0.0016 |      40 B |        1.00 |
| FromBytes            |   856.573 ns | 1.3627 ns | 1.9975 ns | 116.02 |    3.05 | 0.0267 |     688 B |       17.20 |
| ToBytes              | 1,196.953 ns | 3.8062 ns | 5.5791 ns | 162.13 |    4.30 | 0.0191 |     504 B |       12.60 |
