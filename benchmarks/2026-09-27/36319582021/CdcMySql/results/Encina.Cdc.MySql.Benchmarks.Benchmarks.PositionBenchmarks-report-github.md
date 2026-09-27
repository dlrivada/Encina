```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean         | Error       | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-------------:|------------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |     4.001 ns |   0.4707 ns | 0.0258 ns |   0.44 |    0.00 |      - |         - |        0.00 |
| CreateGtidPosition   |     9.116 ns |   1.0669 ns | 0.0585 ns |   1.00 |    0.01 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   798.139 ns |  21.2891 ns | 1.1669 ns |  87.56 |    0.50 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,211.173 ns | 111.2759 ns | 6.0994 ns | 132.87 |    0.94 | 0.0286 |     504 B |       12.60 |
