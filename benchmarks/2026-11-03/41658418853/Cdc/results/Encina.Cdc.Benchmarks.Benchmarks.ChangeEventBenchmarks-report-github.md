```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean       | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |-----------:|----------:|----------:|------:|-------:|----------:|------------:|
| CreateChangeEvent          | 17.8440 ns | 0.3503 ns | 0.0192 ns |  0.63 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  0.9813 ns | 0.0593 ns | 0.0032 ns |  0.03 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 10.9211 ns | 6.7155 ns | 0.3681 ns |  0.39 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 28.1501 ns | 1.2129 ns | 0.0665 ns |  1.00 | 0.0033 |      56 B |        1.00 |
