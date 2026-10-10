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
| CompareFilePositions |     3.971 ns |  0.1390 ns | 0.0076 ns |   0.50 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |     7.885 ns |  1.7902 ns | 0.0981 ns |   1.00 |    0.02 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   782.761 ns | 49.8495 ns | 2.7324 ns |  99.29 |    1.11 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,161.537 ns | 78.9102 ns | 4.3253 ns | 147.33 |    1.66 | 0.0286 |     504 B |       12.60 |
