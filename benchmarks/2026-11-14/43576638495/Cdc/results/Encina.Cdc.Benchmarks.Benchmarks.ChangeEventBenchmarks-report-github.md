```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean       | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |-----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreateChangeEvent          | 20.0908 ns | 3.8115 ns | 0.2089 ns |  0.72 |    0.01 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  0.9837 ns | 0.1213 ns | 0.0067 ns |  0.04 |    0.00 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 11.5543 ns | 9.3389 ns | 0.5119 ns |  0.42 |    0.02 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 27.7562 ns | 3.6687 ns | 0.2011 ns |  1.00 |    0.01 | 0.0033 |      56 B |        1.00 |
