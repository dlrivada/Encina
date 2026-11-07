```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.27GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev    | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     19.73 ns |     0.919 ns |  0.050 ns |     1.27 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     15.56 ns |     2.028 ns |  0.111 ns |     1.00 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 17,622.02 ns | 1,090.304 ns | 59.763 ns | 1,132.68 |    7.75 | 0.3052 | 0.2747 |    5287 B |       47.21 |
| GenericTypeConstruction |     66.30 ns |    31.138 ns |  1.707 ns |     4.26 |    0.10 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     43.14 ns |     8.106 ns |  0.444 ns |     2.77 |    0.03 | 0.0105 |      - |     176 B |        1.57 |
