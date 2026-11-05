```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.27GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean         | Error      | StdDev    | Gen0   | Allocated |
|----------------- |-------------:|-----------:|----------:|-------:|----------:|
| ComparePositions |     1.752 ns |  0.6051 ns | 0.0332 ns |      - |         - |
| FromBytes        | 1,215.064 ns | 69.0224 ns | 3.7834 ns | 0.0439 |     744 B |
| ToBytes          |   516.487 ns | 92.2993 ns | 5.0592 ns | 0.0153 |     264 B |
