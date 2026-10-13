```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev       | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-------------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     28.41 ns |     17.98 ns |     0.986 ns |     1.27 |    0.07 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     22.39 ns |     20.71 ns |     1.135 ns |     1.00 |    0.06 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 53,444.76 ns | 41,725.18 ns | 2,287.098 ns | 2,390.50 |  135.12 | 0.3052 | 0.2441 |    5287 B |       47.21 |
| GenericTypeConstruction |    102.84 ns |     82.12 ns |     4.501 ns |     4.60 |    0.26 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     68.73 ns |     40.59 ns |     2.225 ns |     3.07 |    0.16 | 0.0105 |      - |     176 B |        1.57 |
