```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     18.92 ns |     2.448 ns |   0.134 ns |     1.27 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     14.86 ns |     1.235 ns |   0.068 ns |     1.00 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 15,728.76 ns | 2,365.699 ns | 129.672 ns | 1,058.24 |    8.64 | 0.3052 | 0.2899 |    5287 B |       47.21 |
| GenericTypeConstruction |     61.07 ns |    18.932 ns |   1.038 ns |     4.11 |    0.06 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     40.21 ns |     6.023 ns |   0.330 ns |     2.71 |    0.02 | 0.0105 |      - |     176 B |        1.57 |
