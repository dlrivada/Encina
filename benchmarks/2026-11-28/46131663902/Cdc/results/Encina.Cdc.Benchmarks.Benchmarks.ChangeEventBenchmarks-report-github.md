```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.19GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean       | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |-----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreateChangeEvent          | 13.4586 ns | 9.0067 ns | 0.4937 ns |  0.51 |    0.02 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  0.4767 ns | 0.2633 ns | 0.0144 ns |  0.02 |    0.00 |      - |         - |        0.00 |
| ChangeEvent_WithExpression |  8.1865 ns | 0.9520 ns | 0.0522 ns |  0.31 |    0.00 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 26.1943 ns | 5.8137 ns | 0.3187 ns |  1.00 |    0.01 | 0.0033 |      56 B |        1.00 |
