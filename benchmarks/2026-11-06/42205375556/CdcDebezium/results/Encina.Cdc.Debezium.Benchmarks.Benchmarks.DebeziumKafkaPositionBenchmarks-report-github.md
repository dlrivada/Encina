```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean         | Error       | StdDev    | Gen0   | Allocated |
|----------------- |-------------:|------------:|----------:|-------:|----------:|
| ComparePositions |     3.467 ns |   0.1139 ns | 0.0062 ns |      - |         - |
| FromBytes        | 2,054.797 ns | 104.2238 ns | 5.7129 ns | 0.0420 |     744 B |
| ToBytes          |   933.235 ns |  33.4838 ns | 1.8354 ns | 0.0153 |     264 B |
