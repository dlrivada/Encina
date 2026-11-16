```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.55GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean       | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |-----------:|----------:|----------:|------:|-------:|----------:|------------:|
| CreateChangeEvent          | 18.0080 ns | 0.9112 ns | 0.0499 ns |  0.64 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  0.9899 ns | 0.4093 ns | 0.0224 ns |  0.03 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 10.8903 ns | 5.7015 ns | 0.3125 ns |  0.38 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 28.3412 ns | 1.8932 ns | 0.1038 ns |  1.00 | 0.0033 |      56 B |        1.00 |
