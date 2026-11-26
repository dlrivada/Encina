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
| CreateChangeEvent          | 18.5525 ns | 2.7577 ns | 0.1512 ns |  0.65 |    0.01 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  0.9825 ns | 0.0204 ns | 0.0011 ns |  0.03 |    0.00 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 10.5865 ns | 3.3032 ns | 0.1811 ns |  0.37 |    0.01 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 28.3716 ns | 6.8416 ns | 0.3750 ns |  1.00 |    0.02 | 0.0033 |      56 B |        1.00 |
