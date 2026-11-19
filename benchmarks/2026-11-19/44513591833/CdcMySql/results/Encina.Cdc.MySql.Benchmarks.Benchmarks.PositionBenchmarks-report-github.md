```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.59GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean         | Error      | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-------------:|-----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |     4.003 ns |  0.6999 ns | 0.0384 ns |   0.45 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |     8.844 ns |  1.8572 ns | 0.1018 ns |   1.00 |    0.01 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   780.332 ns | 62.3184 ns | 3.4159 ns |  88.25 |    0.95 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,174.599 ns | 97.7929 ns | 5.3604 ns | 132.83 |    1.43 | 0.0286 |     504 B |       12.60 |
