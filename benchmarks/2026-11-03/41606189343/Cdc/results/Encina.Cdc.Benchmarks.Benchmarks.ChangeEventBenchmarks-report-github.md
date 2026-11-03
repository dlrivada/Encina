```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.13GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean       | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |-----------:|----------:|----------:|------:|-------:|----------:|------------:|
| CreateChangeEvent          | 13.9379 ns | 2.2974 ns | 0.1259 ns |  0.53 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  0.4883 ns | 0.3743 ns | 0.0205 ns |  0.02 |      - |         - |        0.00 |
| ChangeEvent_WithExpression |  8.5322 ns | 1.5420 ns | 0.0845 ns |  0.32 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 26.3929 ns | 4.2732 ns | 0.2342 ns |  1.00 | 0.0033 |      56 B |        1.00 |
