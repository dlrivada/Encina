```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |   3.036 ns |  0.2749 ns | 0.0151 ns |  0.32 |    0.00 |      - |         - |        0.00 |
| CreateGtidPosition   |   9.464 ns |  1.8761 ns | 0.1028 ns |  1.00 |    0.01 | 0.0005 |      40 B |        1.00 |
| FromBytes            | 577.076 ns | 23.6881 ns | 1.2984 ns | 60.98 |    0.59 | 0.0076 |     688 B |       17.20 |
| ToBytes              | 673.826 ns | 29.4175 ns | 1.6125 ns | 71.20 |    0.69 | 0.0057 |     504 B |       12.60 |
