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
| CompareFilePositions |     3.971 ns |  0.0133 ns | 0.0007 ns |   0.50 |    0.00 |      - |         - |        0.00 |
| CreateGtidPosition   |     7.876 ns |  1.0109 ns | 0.0554 ns |   1.00 |    0.01 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   755.982 ns | 59.9188 ns | 3.2844 ns |  95.98 |    0.69 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,130.063 ns | 33.4418 ns | 1.8331 ns | 143.48 |    0.89 | 0.0286 |     504 B |       12.60 |
