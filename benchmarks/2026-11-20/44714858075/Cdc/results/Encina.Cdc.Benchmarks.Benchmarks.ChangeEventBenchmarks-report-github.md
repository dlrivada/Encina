```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.53GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |-----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreateChangeEvent          | 13.2538 ns | 11.1201 ns | 0.6095 ns |  0.56 |    0.02 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  0.4622 ns |  0.0981 ns | 0.0054 ns |  0.02 |    0.00 |      - |         - |        0.00 |
| ChangeEvent_WithExpression |  7.4462 ns |  3.0305 ns | 0.1661 ns |  0.31 |    0.01 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 23.6757 ns |  1.7934 ns | 0.0983 ns |  1.00 |    0.01 | 0.0033 |      56 B |        1.00 |
