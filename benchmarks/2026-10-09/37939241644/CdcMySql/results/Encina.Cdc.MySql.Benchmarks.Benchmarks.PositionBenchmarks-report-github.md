```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean         | Error      | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-------------:|-----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |     4.099 ns |   3.728 ns | 0.2043 ns |   0.51 |    0.02 |      - |         - |        0.00 |
| CreateGtidPosition   |     7.981 ns |   1.956 ns | 0.1072 ns |   1.00 |    0.02 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   775.635 ns | 116.837 ns | 6.4043 ns |  97.19 |    1.33 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,200.633 ns |  50.583 ns | 2.7726 ns | 150.45 |    1.78 | 0.0286 |     504 B |       12.60 |
