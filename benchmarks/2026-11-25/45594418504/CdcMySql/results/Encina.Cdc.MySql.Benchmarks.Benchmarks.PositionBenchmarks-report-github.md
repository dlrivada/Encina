```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean       | Error       | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-----------:|------------:|----------:|------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |   3.017 ns |   0.1021 ns | 0.0056 ns |  0.41 |    0.00 |      - |         - |        0.00 |
| CreateGtidPosition   |   7.297 ns |   1.7082 ns | 0.0936 ns |  1.00 |    0.02 | 0.0005 |      40 B |        1.00 |
| FromBytes            | 561.808 ns | 103.8466 ns | 5.6922 ns | 77.00 |    1.09 | 0.0076 |     688 B |       17.20 |
| ToBytes              | 668.049 ns |  39.5809 ns | 2.1696 ns | 91.56 |    1.06 | 0.0057 |     504 B |       12.60 |
