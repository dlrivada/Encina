```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.67GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean       | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |-----------:|----------:|----------:|------:|-------:|----------:|------------:|
| CreateChangeEvent          | 18.0292 ns | 1.2734 ns | 0.0698 ns |  0.65 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  0.9820 ns | 0.1242 ns | 0.0068 ns |  0.04 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 10.7382 ns | 2.4653 ns | 0.1351 ns |  0.38 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 27.9056 ns | 0.4286 ns | 0.0235 ns |  1.00 | 0.0033 |      56 B |        1.00 |
