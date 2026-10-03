```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 3.19GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean         | Error      | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-------------:|-----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |     3.974 ns |  0.0167 ns | 0.0009 ns |   0.47 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |     8.399 ns |  2.2921 ns | 0.1256 ns |   1.00 |    0.02 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   774.594 ns | 53.1208 ns | 2.9117 ns |  92.24 |    1.24 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,175.163 ns | 59.7598 ns | 3.2756 ns | 139.94 |    1.85 | 0.0286 |     504 B |       12.60 |
