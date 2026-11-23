```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.09GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean         | Error      | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-------------:|-----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |     3.993 ns |  0.4965 ns | 0.0272 ns |   0.50 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |     7.969 ns |  1.6316 ns | 0.0894 ns |   1.00 |    0.01 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   746.941 ns | 62.5491 ns | 3.4285 ns |  93.73 |    0.99 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,141.456 ns | 33.6156 ns | 1.8426 ns | 143.24 |    1.41 | 0.0286 |     504 B |       12.60 |
