```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.65GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean         | Error       | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-------------:|------------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |     3.968 ns |   0.1214 ns | 0.0067 ns |   0.49 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |     8.172 ns |   2.2912 ns | 0.1256 ns |   1.00 |    0.02 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   767.233 ns |  41.9384 ns | 2.2988 ns |  93.90 |    1.28 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,189.636 ns | 133.5603 ns | 7.3209 ns | 145.60 |    2.10 | 0.0286 |     504 B |       12.60 |
