```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean         | Error      | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-------------:|-----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |     3.998 ns |  0.5245 ns | 0.0288 ns |   0.49 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |     8.137 ns |  3.9680 ns | 0.2175 ns |   1.00 |    0.03 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   748.271 ns | 10.0222 ns | 0.5494 ns |  92.00 |    2.16 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,145.884 ns | 28.2849 ns | 1.5504 ns | 140.89 |    3.31 | 0.0286 |     504 B |       12.60 |
