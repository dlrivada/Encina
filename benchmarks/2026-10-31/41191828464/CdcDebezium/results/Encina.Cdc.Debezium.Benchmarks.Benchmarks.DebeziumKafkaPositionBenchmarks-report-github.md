```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean         | Error      | StdDev    | Gen0   | Allocated |
|----------------- |-------------:|-----------:|----------:|-------:|----------:|
| ComparePositions |     2.694 ns |  0.1679 ns | 0.0092 ns |      - |         - |
| FromBytes        | 1,477.525 ns | 93.6617 ns | 5.1339 ns | 0.0439 |     744 B |
| ToBytes          |   679.875 ns | 68.9782 ns | 3.7809 ns | 0.0153 |     264 B |
